using System.ComponentModel.DataAnnotations;

namespace Models.Shared.Enums;

public enum CurrentAudioLanguages
{
    [Display(Name = "en")] 
    English,
    
    [Display(Name = "fa")] 
    Persian,
    
    [Display(Name = "fr")]
    French,
    
    [Display(Name = "ms")]
    Malay,
    
    [Display(Name = "es")]
    Spanish,
    
    [Display(Name = "de")]
    German,
    
    [Display(Name = "zh")]
    Chinese,
    
    [Display(Name = "ja")]
    Japanese,
    
    [Display(Name = "ar")]
    Arabic,
    
    [Display(Name = "hi")]
    Hindi,
    
    [Display(Name = "pt")]
    Portuguese,
    
    [Display(Name = "ru")]
    Russian,
    
    [Display(Name = "ko")]
    Korean,
    
    [Display(Name = "it")]
    Italian,
    
    [Display(Name = "Auto")]
    AutoDetect
}