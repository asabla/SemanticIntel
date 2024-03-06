using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;

using SemanticIntel.Core.Chat;
using SemanticIntel.Core.Memory.Models;
using SemanticIntel.Core.Memory.Extensions;

namespace SemanticIntel.Core.Memory;

public class ApplicationMemoryService(
    ILogger<ApplicationMemoryService> logger,
    IKernelMemory kernelMemory,
    ChatService chatService)
{
    public async Task<string> ImportAsync(
        Stream streamContent,
        string? fileName = null,
        string? documentId = null,
        IEnumerable<UploadTag>? tags = null,
        string? index = null)
    {
        logger.LogDebug(
            message: "Importing document {FileName} with documentId {DocumentId}.",
            args: [fileName, documentId]);

        logger.LogDebug(
            message: "File size: {FileSize}.",
            args: streamContent.Length);

        streamContent.Position = 0;

        documentId = await kernelMemory.ImportDocumentAsync(
            content: streamContent,
            fileName: fileName,
            documentId: documentId,
            tags: tags.ToTagCollection(),
            index: index);

        logger.LogTrace("Document {DocumentId} queued for import.", documentId);

        return documentId;
    }

    public async Task<string> ImportUrlAsync(
        string url,
        string? documentId = null,
        IEnumerable<UploadTag>? tags = null,
        string? index = null)
    {
        documentId = await kernelMemory.ImportWebPageAsync(
            url: url,
            documentId: documentId,
            tags: tags.ToTagCollection(),
            index: index);

        logger.LogTrace("Document {DocumentId} queued for import.", documentId);

        return documentId;
    }

    public async Task<DataPipelineStatus> GetDocumentStatusAsync(
            string documentId,
            string? index = null)
        => await kernelMemory.GetDocumentStatusAsync(documentId, index) ?? null!;

    public async Task DeleteDocumentAsync(
            string documentId,
            string? index = null)
        => await kernelMemory.DeleteDocumentAsync(documentId, index);

    public async Task<MemoryResponse?> AskQuestionAsync(
        Models.Question question,
        double minimumRelevance = 0.76,
        string? index = null)
    {
        logger.LogDebug(
            message: "Asking question {Question} with minimum relevance {MinimumRelevance}.", 
            args: [question.Text, minimumRelevance]);

        // Reformulate the following question taking into account the context of the chat ot perform keyword search and embeddings
        var reformulatedQuestion = await chatService.CreateQuestionAsync(
            conversationId: question.ConversationId,
            question: question.Text);

        logger.LogDebug(
            message: "Reformulated question: {ReformulatedQuestion}.", 
            args: reformulatedQuestion);

        // TODO: save the reformulated question in the chat memory based on the conversationId
        // and passed boolean flag

        // Ask using the embedding search via Kernel Memory and the reformulated question.
        // If tags are provided, use them as filters with OR logic.
        var answer = await kernelMemory.AskAsync(
            question: reformulatedQuestion,
            index: index,
            filters: question.Tags.ToMemoryFilters(),
            minRelevance: minimumRelevance);

        // If you want to use an AND logic for the filters, use the following code:
        // var answer = await kernelMemory.AskAsync(
        //     question: reformulatedQuestion,
        //     index: index,
        //     filter: question.Tags.ToMemoryFilter(),
        //     minRelevance: minimumRelevance);

        if (answer.NoResult == false)
        {
            logger.LogTrace(
                message: "Answer found for question {Question}.",
                args: question.Text);
            logger.LogTrace(
                message: "Answer: {Answer}.",
                args: answer.Result);

            await chatService.AddInteractionAsync(
                conversationId: question.ConversationId,
                question: question.Text,
                answer: answer.Result);

            logger.LogDebug(
                message: "Question proccsed and answer was added to conversation {ConversationId}.",
                args: question.ConversationId);

            return new MemoryResponse(
                Answer: answer.Result,
                Tags: answer.RelevantSources);
        }

        logger.LogDebug(
            message: "No answer found fo question {Question}.",
            args: question.Text);

        return null;
    }
}