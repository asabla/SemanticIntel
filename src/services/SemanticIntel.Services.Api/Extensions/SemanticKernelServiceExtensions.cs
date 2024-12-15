using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
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

        // read more here: https://github.com/microsoft/kernel-memory
        var config = new OllamaConfig
        {
            Endpoint = "http://localhost:11434",
            TextModel = new OllamaModelConfig("qwen2.5:7b", 8000),
            EmbeddingModel = new OllamaModelConfig("mxbai-embed-large:latest", 2048),
        };

        var kernelMemory = new KernelMemoryBuilder()
            .WithOllamaTextGeneration(config)
            .WithOllamaTextEmbeddingGeneration(config)
            .WithQdrantMemoryDb(new()
            {
                Endpoint = "http://localhost:6333"
            })
            .WithSearchClientConfig(new()
            {
                EmptyAnswer = "I'm sorry, I couldn't find any relevant information that could answer your question",
                MaxMatchesCount = 5,
                AnswerTokens = 800
            })
            .WithCustomTextPartitioningOptions(new()
            {
                MaxTokensPerParagraph = options.CustomTextPartitionOptions.MaxTokensPerParagraph,
                MaxTokensPerLine = options.CustomTextPartitionOptions.MaxTokenPerLine,
                OverlappingTokens = options.CustomTextPartitionOptions.OverlappingTokens
            })
            .Build();

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        builder.Services.AddKernel()
            .AddOllamaChatCompletion(
                modelId: config.TextModel.ModelName,
                endpoint: new Uri(config.Endpoint));
#pragma warning restore SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        builder.Services.AddSingleton<IKernelMemory>(kernelMemory);
        builder.Services.AddScoped<ChatService>();
        builder.Services.AddScoped<ApplicationMemoryService>();

        return builder;
    }
}