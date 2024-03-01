using Microsoft.KernelMemory;

namespace SemanticIntel.Core.Memory.Models;

public record class MemoryResponse(string Answer, IEnumerable<Citation> Tags);