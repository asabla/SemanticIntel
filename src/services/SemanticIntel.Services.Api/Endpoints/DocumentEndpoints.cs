using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.KernelMemory;

using SemanticIntel.Core.Memory;
using SemanticIntel.Core.Memory.Models;

namespace SemanticIntel.Services.Api.Endpoints;

/// <summary>
/// Provides the document endpoints for the application.
/// </summary>
/// <remarks>
/// The document endpoints allow you to import, get status, and delete documents.
/// </remarks>
internal static class DocumentEndpoints
{
    // <summary>
    // Adds the document endpoints to the application.
    // </summary>
    // <param name="app">The application to add the endpoints to.</param>
    // <returns>The application with the added endpoints.</returns>
    // <remarks>
    // The document endpoints allow you to import, get status, and delete documents.
    // </remarks>
    public static WebApplication AddDocumentEndpoints(
        this WebApplication app)
    {
        var group = app.MapGroup("/api/documents");

        // TODO: add alternative for importing a document from a URL
        //          while using playwright instead of the kernel memory service
        app.MapPostUploadDocument()
            .MapPostUrl()
            .MapGetDocumentStatus()
            .MapDeleteDocument()
            .MapPostAskQuestion();

        return app;
    }

    /// <summary>
    /// Adds the get document status endpoint to the application.
    /// </summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns>The application with the added endpoint.</returns>
    /// <remarks>
    /// The get document status endpoint allows you to get the status of a document in the memory service.
    /// </remarks>
    private static WebApplication MapPostUploadDocument(
        this WebApplication app)
    {
        app.MapPost(
            pattern: string.Empty,
            handler: async (
                ApplicationMemoryService memoryService,
                LinkGenerator linkGenerator,
                IFormFile file,
                string? documentId = null,
                string? index = null,
                [FromQuery(Name = "tag")] UploadTag[]? tags = null) =>
                {
                    documentId = await memoryService.ImportAsync(
                        streamContent: file.OpenReadStream(),
                        fileName: file.FileName,
                        documentId: documentId,
                        tags: tags,
                        index: index);

                    var uri = linkGenerator.GetPathByName("GetDocumentStatus", new { documentId });

                    return TypedResults.Accepted(uri, new UploadDocumentResponse(documentId));
                })
            .DisableAntiforgery()
            .WithName("ImportDocument")
            .WithOpenApi(operation =>
            {
                var documentId = operation.Parameters.First(p => p.Name == "documentId");
                var index = operation.Parameters.First(p => p.Name == "index");
                var tags = operation.Parameters.First(p => p.Name == "tag");

                documentId.Description = "The unique identifier of the document. If not provided, a new one will be generated. If you specify an existing documentId, the document will be overriden";
                index.Description = "The index to use for the document. If not provided, the default index will be used ('default')";
                tags.Description = "The tags to associate with the document. Use the format 'tag=tagName:tagValue' to define a tag (i.e. ?tag=userId:123&tag=name:someName)";

                return operation;
            });

        return app;
    }

    /// <summary>
    /// Adds the get document status endpoint to the application.
    /// </summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns>The application with the added endpoint.</returns>
    /// <remarks>
    /// The get document status endpoint allows you to get the status of a document in the memory service.
    /// </remarks>
    private static WebApplication MapPostUrl(
        this WebApplication app)
    {
        app.MapPost(
            pattern: "url",
            handler: async (
                ApplicationMemoryService memoryService,
                LinkGenerator linkGenerator,
                Uri url,
                string? documentId = null,
                string? index = null,
                [FromQuery(Name = "tag")] UploadTag[]? tags = null) =>
            {
                documentId = await memoryService.ImportUrlAsync(
                    url: url.ToString(),
                    documentId: documentId,
                    tags: tags,
                    index: index);

                var uri = linkGenerator.GetPathByName("GetDocumentStatus", new { documentId });

                return TypedResults.Accepted(uri, new UploadDocumentResponse(documentId));
            })
        .WithName("ImportDocumentFromUrl")
        .WithOpenApi(operation =>
        {
            var documentId = operation.Parameters.First(p => p.Name == "documentId");
            var index = operation.Parameters.First(p => p.Name == "index");
            var tags = operation.Parameters.First(p => p.Name == "tag");

            documentId.Description = "The unique identifier of the document. If not provided, a new one will be generated. If you specify an existing documentId, the document will be overriden";
            index.Description = "The index to use for the document. If not provided, the default index will be used ('default')";
            tags.Description = "The tags to associate with the document. Use the format 'tag=tagName:tagValue' to define a tag (i.e. ?tag=userId:123&tag=name:someName)";

            return operation;
        });

        return app;
    }

    /// <summary>
    /// Adds the get document status endpoint to the application.
    /// </summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns>The application with the added endpoint.</returns>
    /// <remarks>
    /// The get document status endpoint allows you to get the status of a document in the memory service.
    /// </remarks>
    private static WebApplication MapGetDocumentStatus(
        this WebApplication app)
    {
        app.MapGet(
            pattern: "{documentId}/status",
            handler: async Task<Results<Ok<DataPipelineStatus>, NotFound>> (
                ApplicationMemoryService memoryService,
                string documentId,
                string? index = null) =>
            {
                var status = await memoryService.GetDocumentStatusAsync(documentId, index);
                if (status is null)
                    return TypedResults.NotFound();

                return TypedResults.Ok(status);
            })
        .WithName("GetDocumentStatus")
        .WithOpenApi(operation =>
        {
            var documentId = operation.Parameters.First(p => p.Name == "documentId");
            var index = operation.Parameters.First(p => p.Name == "index");

            documentId.Description = "The unique identifier of the document";
            index.Description = "The index to use for the document. If not provided, the default index will be used ('default')";

            return operation;
        });

        return app;
    }

    /// <summary>
    /// Adds the delete document endpoint to the application.
    /// </summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns>The application with the added endpoint.</returns>
    /// <remarks>
    /// The delete document endpoint allows you to delete a document from the memory service.
    /// </remarks>
    private static WebApplication MapDeleteDocument(
        this WebApplication app)
    {
        app.MapDelete(
            pattern: "{documentId}",
            handler: async (
                ApplicationMemoryService memoryService,
                string documentId,
                string? index = null) =>
            {
                await memoryService.DeleteDocumentAsync(documentId, index);
                return TypedResults.NoContent();
            })
            .WithName("DeleteDocument")
            .WithOpenApi(operation =>
            {
                var documentId = operation.Parameters.First(p => p.Name == "documentId");
                var index = operation.Parameters.First(p => p.Name == "index");

                documentId.Description = "The unique identifier of the document";
                index.Description = "The index to use for the document. If not provided, the default index will be used ('default')";

                return operation;
            });

        return app;
    }

    /// <summary>
    /// Adds the ask question endpoint to the application.
    /// </summary>
    /// <param name="app">The application to add the endpoint to.</param>
    /// <returns>The application with the added endpoint.</returns>
    /// <remarks>
    /// The ask question endpoint allows you to ask a question to the memory service.
    /// </remarks>
    private static WebApplication MapPostAskQuestion(
        this WebApplication app)
    {
        // TODO: move to a different file and group (maybe askEndpoints)
        app.MapPost(
            pattern: "/api/ask",
            handler: async Task<Results<Ok<MemoryResponse>, NotFound>> (
                ApplicationMemoryService memoryService,
                Question question,
                double minimumRelevance = 0.76,
                string? index = null) =>
            {
                var response = await memoryService.AskQuestionAsync(
                    question: question,
                    minimumRelevance: minimumRelevance,
                    index: index);

                if (response is null)
                    return TypedResults.NotFound();

                return TypedResults.Ok(response);
            })
            .WithName("AskQuestion")
            .WithOpenApi(operation =>
            {
                var minimumRelevance = operation.Parameters.First(p => p.Name == "minimumRelevance");
                var index = operation.Parameters.First(p => p.Name == "index");

                minimumRelevance.Description = "The minimum relevance score for the response. If not provided, the default value will be used (0.76)";
                index.Description = "The index to use for the document. If not provided, the default index will be used ('default')";

                return operation;
            });

        return app;
    }
}