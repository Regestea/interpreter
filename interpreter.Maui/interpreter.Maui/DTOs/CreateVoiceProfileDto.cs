using System.ComponentModel.DataAnnotations;

namespace interpreter.Maui.DTOs;

public class CreateVoiceProfileDto
{
    public string Name { get; set; }
    
    public Stream Voice { get; set; }
}