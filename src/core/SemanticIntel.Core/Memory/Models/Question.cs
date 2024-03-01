namespace SemanticIntel.Core.Memory.Models;

public record Question(Guid ConversationId, string Text, IEnumerable<Tag> Tags);