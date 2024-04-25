using SemanticIntel.Services.Api.Endpoints;
using SemanticIntel.Services.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddUserSecrets<Program>()
    .AddJsonFile(
        path: "appsettings.json",
        optional: true,
        reloadOnChange: true)
    .AddJsonFile(
        path: $"appsettings.{builder.Environment.EnvironmentName}.json",
        optional: true,
        reloadOnChange: true)
    .AddEnvironmentVariables();

builder
    .AddSemanticKernelServices(
        new SemanticKernelOptions(
            EmbeddingOptions: new DeploymentOptions(
                Endpoint: builder.Configuration["SemanticKernel:Embedding:Endpoint"] ?? string.Empty,
                Deployment: builder.Configuration["SemanticKernel:Embedding:Deployment"] ?? string.Empty,
                ApiKey: builder.Configuration["SemanticKernel:Embedding:ApiKey"] ?? string.Empty),
            TextGenerationOptions: new DeploymentOptions(
                Endpoint: builder.Configuration["SemanticKernel:TextGeneration:Endpoint"] ?? string.Empty,
                Deployment: builder.Configuration["SemanticKernel:TextGeneration:Deployment"] ?? string.Empty,
                ApiKey: builder.Configuration["SemanticKernel:TextGeneration:ApiKey"] ?? string.Empty),
            ChatCompletionOptions: new DeploymentOptions(
                Endpoint: builder.Configuration["SemanticKernel:ChatCompletion:Endpoint"] ?? string.Empty,
                Deployment: builder.Configuration["SemanticKernel:ChatCompletion:Deployment"] ?? string.Empty,
                ApiKey: builder.Configuration["SemanticKernel:ChatCompletion:ApiKey"] ?? string.Empty),
            QdrantOptions: new QdrantOptions(
                Endpoint: builder.Configuration["SemanticKernel:Qdrant:Endpoint"] ?? string.Empty),
            CustomTextPartitionOptions: new CustomTextPartitionOptions()))
    .AddSwagger();

var app = builder.Build()
    .AddSwagger()
    .AddDocumentEndpoints();

app.Run();