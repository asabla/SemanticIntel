using Microsoft.KernelMemory;

using SemanticIntel.Core.Memory.Models;

namespace SemanticIntel.Core.Memory.Extensions;

public static class TagExtensions
{
    public static TagCollection? ToTagCollection(this IEnumerable<UploadTag>? tags)
    {
        // TODO: fix this
        if (tags.IsNullOrEmpty())
            return null;

        // TODO: fix null warning
        var collection = new TagCollection();
        foreach (var tag in tags)
            collection.Add(tag.Name, tag.Value);

        return collection;
    }

    /// <summary>
    /// Creates a single filter that searches for all the tags in the collection using AND logic.
    /// </summary>
    public static MemoryFilter? ToMemoryFilter(this IEnumerable<Tag>? tags)
    {
        if (tags.IsNullOrEmpty())
            return null;

        // TODO: fix null warning
        var memoryFilter = new MemoryFilter();
        foreach (var tag in tags)
            memoryFilter.Add(tag.Name, tag.Value);

        return memoryFilter;
    }

    public static ICollection<MemoryFilter>? ToMemoryFilters(this IEnumerable<Tag>? tags)
    {
        if (tags.IsNullOrEmpty())
            return null;

        // TODO: fix null warning
        var memoryFilters = new List<MemoryFilter>();
        foreach (var tag in tags)
            memoryFilters.Add(MemoryFilters.ByTag(tag.Name, tag.Value));

        return memoryFilters;
    }

    public static bool IsNullOrEmpty(this IEnumerable<UploadTag>? tags)
        => tags is null || tags.Any() is false;

    public static bool IsNullOrEmpty(this IEnumerable<Tag>? tags)
        => tags is null || tags.Any() is false;
}