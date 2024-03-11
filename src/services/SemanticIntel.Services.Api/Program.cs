using SemanticIntel.Services.Api.Endpoints;
using SemanticIntel.Services.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder
    .AddSemanticKernelServices(
        new SemanticKernelOptions(
            EmbeddingOptions: new DeploymentOptions(
                Endpoint: builder.Configuration["SemanticKernel:Embedding:Endpoint"] ?? string.Empty,
                Deployment: builder.Configuration["SemanticKernel:Embedding:Deployment"] ?? string.Empty),
            TextGenerationOptions: new DeploymentOptions(
                Endpoint: builder.Configuration["SemanticKernel:TextGeneration:Endpoint"] ?? string.Empty,
                Deployment: builder.Configuration["SemanticKernel:TextGeneration:Deployment"] ?? string.Empty),
            ChatCompletionOptions: new DeploymentOptions(
                Endpoint: builder.Configuration["SemanticKernel:ChatCompletion:Endpoint"] ?? string.Empty,
                Deployment: builder.Configuration["SemanticKernel:ChatCompletion:Deployment"] ?? string.Empty),
            QdrantOptions: new QdrantOptions(
                Endpoint: builder.Configuration["SemanticKernel:Qdrant:Endpoint"] ?? string.Empty),
            CustomTextPartitionOptions: new CustomTextPartitionOptions()))
    .AddSwagger();

var app = builder.Build()
    .AddSwagger()
    .AddDocumentEndpoints();

app.Run();