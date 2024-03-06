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

        var result = chatCompletionService.GetStreamingChatMessageContentsAsync(
                chatHistory: chat);

        logger.LogTrace("Start reformulating question for conversation {ConversationId}", conversationId);

        string reformulatedQuestion = string.Empty;
        await foreach (var message in result)
        {
            reformulatedQuestion += message.Content;
        }

        logger.LogTrace("Reformulated question for conversation {ConversationId}: {ReformulatedQuestion}", conversationId, reformulatedQuestion);

        chat.AddAssistantMessage(reformulatedQuestion);
        await UpdateCacheAsync(conversationId, chat);

        return reformulatedQuestion;
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