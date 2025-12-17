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
    [Required] // Ensure this property is not null
    public string EmbeddingJson { get; set; } 
    
}