using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SemanticIntel.Core.Chat;

public sealed class ChatService(
    ILogger<ChatService> logger,
    IMemoryCache memoryCache,
    IChatCompletionService chatCompletionService)
{
    public async Task<string> CreateQuestionAsync(Guid conversationId, string question)
    {
        var chat = new ChatHistory(
            messages: memoryCache.Get<ChatHistory?>(conversationId) ?? []);

        var embeddingQuestion = $"""
            Reformulate the following question taking into account the context of the chat to perform search:
            ---
            {question}
            ---
            You must reformulate the question in the same language of the user's question.
            Never add "in this chat", "in the context of this chat", "in the context of our conversation", "search for" or something like that in your answer.
            """;

        chat.AddUserMessage(embeddingQuestion);

        var reformulatedQuestion = await chatCompletionService.GetChatMessageContentAsync(chat)!;

        if (string.IsNullOrWhiteSpace(reformulatedQuestion.InnerContent?.ToString()) is true)
        {
            logger.LogWarning("The reformulated question is empty.");
            return string.Empty;
        }
        else
        {
            chat.AddAssistantMessage(reformulatedQuestion.InnerContent.ToString() ?? string.Empty);

            await UpdateCacheAsync(conversationId, chat);

            return reformulatedQuestion.InnerContent.ToString() ?? string.Empty;
        }
    }

    public async Task AddInteractionAsync(
        Guid conversationId,
        string question,
        string answer)
    {
        var chat = new ChatHistory(
            messages: memoryCache.Get<ChatHistory?>(conversationId) ?? []);

        chat.AddUserMessage(question);
        chat.AddAssistantMessage(answer);

        await UpdateCacheAsync(conversationId, chat);
    }

    private Task UpdateCacheAsync(Guid conversationId, ChatHistory chat)
    {
        if (chat.Count > 10)
            chat = new ChatHistory(chat.TakeLast(10));

        memoryCache.Set(conversationId, chat);

        return Task.CompletedTask;
    }
}