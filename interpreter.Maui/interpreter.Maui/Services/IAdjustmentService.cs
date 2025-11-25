namespace interpreter.Maui.Services;

public interface IAdjustmentService
{
    Task AdjustEnvironmentalNoise();
    
    Task TrainModelWithUserVoiceAsync();
}