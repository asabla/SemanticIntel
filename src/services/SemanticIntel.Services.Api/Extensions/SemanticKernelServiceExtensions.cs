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

        var kernelBuilder = builder.Services.AddKernel()
            .AddAzureOpenAIChatCompletion(
                deploymentName: options.ChatCompletionOptions.Deployment,
                endpoint: options.ChatCompletionOptions.Endpoint,
                credentials: new DefaultAzureCredential());

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
                Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
                Deployment = options.EmbeddingOptions.Deployment,
                Endpoint = options.EmbeddingOptions.Endpoint,
                MaxRetries = options.EmbeddingOptions.MaxRetries,
            })
            .WithAzureOpenAITextGeneration(new()
            {
                Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity,
                Deployment = options.TextGenerationOptions.Deployment,
                Endpoint = options.TextGenerationOptions.Endpoint,
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
}