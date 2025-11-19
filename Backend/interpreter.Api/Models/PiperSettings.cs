namespace interpreter.Api.Models
{
    public class PiperSettings
    {
        public string? DefaultModel { get; set; }
        public float SpeakingRate { get; set; } = 1.0f;
        public uint SpeakerId { get; set; } = 0;
        public bool UseCuda { get; set; } = false;
    }
}

