using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace interpreter.Api.Entities;

public class VoiceEmbedding
{
    [Key]
    public Guid Id { get; set; }
    
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;
    
    // Store as JSON in database
    public string EmbeddingJson { get; set; } = string.Empty;
    
    // Use this in memory
    [NotMapped] // If using EF Core
    public List<float> Embedding 
    { 
        get => JsonSerializer.Deserialize<List<float>>(EmbeddingJson) ?? new();
        set => EmbeddingJson = JsonSerializer.Serialize(value);
    }
}