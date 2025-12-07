using System.ComponentModel.DataAnnotations;

namespace Models.Shared.Requests;

public class CreateVoiceDetectorRequest
{
    [Required]
    public string Name { get; set; }
    
    [Required]
    public byte[] Voice { get; set; }
}