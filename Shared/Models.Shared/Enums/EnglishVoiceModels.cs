using System.ComponentModel.DataAnnotations;

namespace Models.Shared.Enums;

public enum EnglishVoiceModels
{
    [Display(Name = "en_US-ryan-high")] 
    EnUsRyanHigh,

    [Display(Name = "en_US-hfc_female-medium")]
    EnUsHfcFemaleMedium,

    [Display(Name = "en_US-amy-medium")] 
    EnUsAmyMedium,

    [Display(Name = "fa_IR-gyro-medium")] 
    FaIrGyroMedium
}