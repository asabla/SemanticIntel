using System.Collections.Concurrent;
using System.Text.Json;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace SemanticIntel.Core.Crawler;

public sealed class CrawlerService(ILogger<CrawlerService> logger)
    : IHostedService
{
    // ------------ TODO: Move into configuration model ------------
    readonly int slowMo = 20;
    readonly int taskTimeout = 50;
    readonly int waitIntervalInSeconds = 5;
    readonly int requestTimeout = 12000;
    readonly int maxDegreeOfParallelism = 8;
    // ------------ TODO: Move into configuration model ------------

    private IPlaywright? playwright;
    private IBrowserContext? browserContext;

    private readonly ConcurrentDictionary<string, bool> visistedUrls = new();
    private readonly ConcurrentQueue<string> urlsToVisit = new();
    private readonly List<string> allowedDomains = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("CrawlerService started");

        playwright = await Playwright.CreateAsync();
        browserContext = await playwright.Chromium.LaunchPersistentContextAsync("./user-data-dir", new()
        {
            // TODO: Move into configuration model
            Headless = true,
            SlowMo = slowMo,
            ScreenSize = new() { Width = 1920, Height = 1080 },
            // TODO: Have this as a default configuration, but allow to override
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36",
        });

        logger.LogTrace("Browser launched");
        logger.LogInformation($"Starting to crawl {urlsToVisit.Count} URLs");

        try
        {
            while (cancellationToken.IsCancellationRequested is false)
            {
                await Parallel.ForEachAsync(
                    source: urlsToVisit,
                    parallelOptions: new() { MaxDegreeOfParallelism = maxDegreeOfParallelism },
                    body: async (url, CancellationToken) =>
                    {
                        string currentUrl = string.Empty;
                        // var page = await browser.NewPageAsync();
                        var page = await browserContext.NewPageAsync();
                        currentUrl = await NavigateAndCheckIfVisistedAsync(
                                page: page,
                                currentUrl: currentUrl);

                        if (IsAllowedUrl(currentUrl) is false || string.IsNullOrWhiteSpace(currentUrl))
                        {
                            logger.LogTrace($"URL {currentUrl} is not allowed or is empty/null, skipping...");
                            await page.CloseAsync();
                            return;
                        }

                        logger.LogInformation($"Visiting URL: {currentUrl}");

                        logger.LogTrace($"Refusing all cookies on {currentUrl}");
                        await RefuseAllCookiesAsync(page);

                        var hrefs = await SaveContentToDiskAsync(page);

                        if (hrefs is null)
                        {
                            logger.LogTrace($"No hrefs found on {currentUrl}, skipping...");
                            await page.CloseAsync();
                            return;
                        }

                        FilterAllowedDomains(allowedDomains, hrefs);

                        foreach (var href in hrefs)
                            urlsToVisit.Enqueue(href);

                        logger.LogTrace($"Added {hrefs.Count} new URLs to the queue");
                        logger.LogTrace($"Current queue size: {urlsToVisit.Count}");
                        logger.LogTrace($"Visisted URL {currentUrl}");

                        await page.CloseAsync();

                        logger.LogInformation($"Visited URL: {currentUrl}");

                        // Keeps crawler from being blocked
                        await Task.Delay(taskTimeout);
                    });

                await Task.Delay(TimeSpan.FromSeconds(waitIntervalInSeconds));
            }
        }
        finally
        {
            // TODO: log everything that was visited
            // TODO: clear out queues and dictionaries
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("CrawlerService stopped");
        playwright?.Dispose();

        return Task.CompletedTask;
    }

    private List<string> FilterAllowedDomains(
            List<string> allowedDomains,
            List<string> hrefs)
        => hrefs.Where(e => allowedDomains.Contains(e) && e.StartsWith("http")).ToList();

    private bool IsAllowedUrl(string url)
        => allowedDomains.Any(e => url.Contains(e));

    private async Task<string> NavigateAndCheckIfVisistedAsync(
        IPage page,
        string currentUrl)
    {
        // Set currentUrl and remove first time from urlsToVisit
        urlsToVisit.TryDequeue(out var url);
        // TODO: add boolean check if dequeue was successful or not

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogTrace("No URL to visit, skipping...");
            return string.Empty;
        }

        currentUrl = url;
        if (IsAllowedUrl(currentUrl) is false)
        {
            logger.LogTrace($"URL {currentUrl} is not allowed, skipping...");
            return string.Empty;
        }

        if (visistedUrls.ContainsKey(currentUrl) && visistedUrls[currentUrl] is true)
        {
            logger.LogTrace($"URL {currentUrl} already visited, skipping...");
            return string.Empty;
        }

        logger.LogTrace($"Navigating to: {currentUrl}");

        try
        {
            await page.GotoAsync(currentUrl, options: new()
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = requestTimeout
            });

            // If current URL was redirected, update the current URL
            if (currentUrl.Equals(page.Url) is false)
            {
                logger.LogTrace($"URL {currentUrl} was redirected to {page.Url}");
                currentUrl = page.Url;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error navigating to {currentUrl}");

            return string.Empty;
        }
        finally
        {
            visistedUrls.AddOrUpdate(currentUrl, true, (_, _) => true);
            logger.LogTrace($"URL {currentUrl} has been marked as visited");
        }

        return currentUrl;
    }

    private async Task RefuseAllCookiesAsync(IPage page)
    {
        var refuseAllSelector = "text='Refuse all'";
        if (await page.IsVisibleAsync(refuseAllSelector))
            await page.ClickAsync(refuseAllSelector);

        // TODO: make this more generic
        var sweAcceptSelected = "text='Godkänn alla kakor'";
        if (await page.IsVisibleAsync(sweAcceptSelected))
            await page.ClickAsync(sweAcceptSelected);
    }

    private async Task<List<string>> FetchAllLinksAsync(
        IPage page,
        string currentUrl)
    {
        logger.LogTrace($"Start working on: {currentUrl}");

        if (string.IsNullOrWhiteSpace(currentUrl))
        {
            logger.LogTrace("No URL to fetch links from, skipping...");
            return new();
        }

        var links = await page.QuerySelectorAllAsync("a");
        var hrefs = new List<string>();

        foreach (var link in links)
        {
            var href = await link.GetAttributeAsync("href");

            if (href is not null && href.Contains("javascript") is false)
            {
                var newHref = href.StartsWith("/")
                    ? new Uri(new Uri(currentUrl), href).ToString()
                    : href;

                // TODO: add logging for this
                // Console.WriteLine($"Adding {newHref} to the list of hrefs");
                hrefs.Add(newHref);

                // hrefs.Add(href.StartsWith("/")
                //     ? Path.Combine(currentUrl, href).ToString()
                //     : href);
            }
        }

        logger.LogTrace($"Found {hrefs.Count} links on {currentUrl}");

        return hrefs;
    }

    private async Task<List<string>> SaveContentToDiskAsync(IPage page)
    {
        try
        {
            // Data to be saved
            var content = await page.ContentAsync();
            var textContent = await page.TextContentAsync("body");    // TODO: clear out scripts, styles, new lines etc.
            var imageTags = await page.QuerySelectorAllAsync("img");
            var hrefs = await FetchAllLinksAsync(page, page.Url);

            // Define the path to save all the data
            var uri = new Uri(page.Url);
            var relativePath = uri.AbsoluteUri.Replace("https:", "").TrimStart('/');
            var domain = relativePath.Split('/').First();
            var filePath = Path.Combine("result", relativePath, $"{relativePath.Replace("/", "_")}.html");

            // Ensure the directory exists
            var directory = Path.GetDirectoryName(filePath);
            if (directory is not null && Directory.Exists(directory) is false)
                Directory.CreateDirectory(directory);

            // Save json content to disk
            var jsonContent = JsonSerializer.Serialize(new
            {
                Url = page.Url,
                Created = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                Images = imageTags.Select(async e => await e.GetAttributeAsync("src")).ToList()
            });

            logger.LogTrace($"Saving content to: {filePath}");
            await Task.WhenAll(
                File.WriteAllTextAsync(filePath, content),
                File.WriteAllTextAsync(filePath.Replace(".html", ".json"), jsonContent),
                File.WriteAllTextAsync(filePath.Replace(".html", ".txt"), textContent),
                page.ScreenshotAsync(new()
                {
                    Path = filePath.Replace(".html", ".png"),
                    FullPage = true
                    // Type = ScreenshotType.Png
                })
            );

            logger.LogTrace($"Content saved to: {filePath}");
            logger.LogTrace($"Found {imageTags.Count} images on {page.Url}");

            return hrefs;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while saving content to disk");
            return null!;
        }
    }
}