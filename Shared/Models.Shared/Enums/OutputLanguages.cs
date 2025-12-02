using System.ComponentModel.DataAnnotations;

namespace Models.Shared.Enums;

public enum OutputLanguages
{
    [Display(Name = "en")] 
    English,
    
    [Display(Name = "fa")] 
    Persian
}