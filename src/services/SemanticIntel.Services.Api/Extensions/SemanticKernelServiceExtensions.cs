using Azure.Identity;

using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.SemanticKernel;

using SemanticIntel.Core.Chat;
using SemanticIntel.Core.Memory;

namespace SemanticIntel.Services.Api.Extensions;

// ---------- SemanticKernelOptions ----------
public record DeploymentOptions(
    string Endpoint,
    string Deployment,
    string? ApiKey = null,
    int MaxRetries = 3);

public record QdrantOptions(
    string Endpoint);

public record CustomTextPartitionOptions(
    int MaxTokensPerParagraph = 1000,
    int MaxTokenPerLine = 300,
    int OverlappingTokens = 100);

public record SemanticKernelOptions(
    DeploymentOptions EmbeddingOptions,
    DeploymentOptions TextGenerationOptions,
    DeploymentOptions ChatCompletionOptions,
    QdrantOptions QdrantOptions,
    CustomTextPartitionOptions CustomTextPartitionOptions);
// ---------- SemanticKernelOptions ----------

internal static class SemanticKernelServiceExtensions
{
    public static WebApplicationBuilder AddSemanticKernelServices(
        this WebApplicationBuilder builder,
        SemanticKernelOptions options)
    {
        builder.Services.AddMemoryCache();

        var kernelMemory = SetupKernelMemory(
            services: builder.Services,
            options: options);

        builder.Services.AddSingleton<IKernelMemory>(kernelMemory);

        if (string.IsNullOrWhiteSpace(options.ChatCompletionOptions.ApiKey))
        {
            builder.Services.AddKernel()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: options.ChatCompletionOptions.Deployment,
                    endpoint: options.ChatCompletionOptions.Endpoint,
                    credentials: new DefaultAzureCredential());
        }
        else
        {
            builder.Services.AddKernel()
                .AddAzureOpenAIChatCompletion(
                    deploymentName: options.ChatCompletionOptions.Deployment,
                    endpoint: options.ChatCompletionOptions.Endpoint,
                    apiKey: options.ChatCompletionOptions.ApiKey);
        }

        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<ApplicationMemoryService>();

        return builder;
    }

    private static IKernelMemory SetupKernelMemory(
            IServiceCollection services,
            SemanticKernelOptions options)
        => new KernelMemoryBuilder(services)
            .WithAzureOpenAITextEmbeddingGeneration(new()
            {
                Auth = options.EmbeddingOptions.ApiKey is null
                    ? AzureOpenAIConfig.AuthTypes.AzureIdentity
                    : AzureOpenAIConfig.AuthTypes.APIKey,
                Deployment = options.EmbeddingOptions.Deployment,
                Endpoint = options.EmbeddingOptions.Endpoint,
                APIKey = options.EmbeddingOptions.ApiKey is null
                    ? string.Empty
                    : options.EmbeddingOptions.ApiKey,
                MaxRetries = options.EmbeddingOptions.MaxRetries,
            })
            .WithAzureOpenAITextGeneration(new()
            {
                // Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
                Auth = options.TextGenerationOptions.ApiKey is null
                    ? AzureOpenAIConfig.AuthTypes.AzureIdentity
                    : AzureOpenAIConfig.AuthTypes.APIKey,
                Deployment = options.TextGenerationOptions.Deployment,
                Endpoint = options.TextGenerationOptions.Endpoint,
                APIKey = options.TextGenerationOptions.ApiKey is null
                    ? string.Empty
                    : options.TextGenerationOptions.ApiKey,
                MaxRetries = options.TextGenerationOptions.MaxRetries,
            })
            .WithQdrantMemoryDb(new()
            {
                Endpoint = options.QdrantOptions.Endpoint
            })
            .WithSearchClientConfig(new()
            {
                EmptyAnswer = "I'm sorry, I couldn't find any relevant information that could answer your question",
                MaxMatchesCount = 5,
                AnswerTokens = 800
            })
            .WithCustomTextPartitioningOptions(new TextPartitioningOptions
            {
                MaxTokensPerParagraph = options.CustomTextPartitionOptions.MaxTokensPerParagraph,
                MaxTokensPerLine = options.CustomTextPartitionOptions.MaxTokenPerLine,
                OverlappingTokens = options.CustomTextPartitionOptions.OverlappingTokens
            })
            .Build();

    // TODO: Remove this method and document it in readme or wiki
    // Used for debugging purposes, and to bypass SSL validation
    // if it occurs again for OpenAI services
    // private static IKernelBuilder AddCustomAzureOpenAIChatCompletion(
    //         this IKernelBuilder builder,
    //         string deploymentName,
    //         string endpoint,
    //         Azure.Core.TokenCredential credential)
    // {
    //     var handler = new HttpClientHandler
    //     {
    //         ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
    //         {
    //             Console.WriteLine("SSL Validation CallBack");
    //             Console.WriteLine($"URL: {request.RequestUri?.ToString()}");
    //             Console.WriteLine($"ERROR: {errors}");
    //
    //             return true;
    //         }
    //     };
    //
    //     var httpClient = new HttpClient(handler);
    //
    //     builder.AddAzureOpenAIChatCompletion(
    //         deploymentName: deploymentName,
    //         endpoint: endpoint,
    //         credentials: credential,
    //         httpClient: httpClient);
    //
    //     return builder;
    // }
}