using Debug = System.Diagnostics.Debug;

namespace interpreter.Maui.Services;

public class AdjustmentService: IAdjustmentService
{
    private readonly IAudioRecorderService _audioRecorder;
    private readonly AudioCalibration _audioCalibration;

    public AdjustmentService(IAudioRecorderService audioRecorder, AudioCalibration audioCalibration)
    {
        _audioRecorder = audioRecorder ?? throw new ArgumentNullException(nameof(audioRecorder));
        _audioCalibration = audioCalibration ?? throw new ArgumentNullException(nameof(audioCalibration));
    }

    public async Task AdjustEnvironmentalNoise()
    {
        // Record 5 seconds of environmental noise
        var recordingDuration = TimeSpan.FromSeconds(5);
        
        await Task.Run(() =>
        {
            using var audioStream = _audioRecorder.RecordForDuration(recordingDuration);
            
            // Skip WAV header (44 bytes)
            audioStream.Seek(44, SeekOrigin.Begin);
            
            // Read audio samples
            var audioData = new List<short>();
            var buffer = new byte[2]; // 16-bit samples = 2 bytes
            
            while (audioStream.Read(buffer, 0, 2) == 2)
            {
                // Convert bytes to 16-bit PCM sample (little-endian)
                short sample = (short)(buffer[0] | (buffer[1] << 8));
                audioData.Add(sample);
            }
            
            // Calculate RMS (Root Mean Square)
            if (audioData.Count > 0)
            {
                double sumOfSquares = 0;
                foreach (var sample in audioData)
                {
                    // Normalize sample to range [-1.0, 1.0]
                    double normalizedSample = sample / 32768.0;
                    sumOfSquares += normalizedSample * normalizedSample;
                }
                
                double meanSquare = sumOfSquares / audioData.Count;
                double rms = Math.Sqrt(meanSquare);
                
                // Convert RMS to dBFS (decibels relative to full scale)
                // dBFS = 20 * log10(RMS)
                double dbfs = 20 * Math.Log10(rms);
                
                // Clamp to integer range between -60 and -15
                int dbfsInt = (int)Math.Round(dbfs);
                dbfsInt = Math.Max(-60, Math.Min(-15, dbfsInt));
                
                // Update calibration
                _audioCalibration.EnvironmentalNoiseRms = rms;
                _audioCalibration.EnvironmentalNoiseDbfs = dbfsInt;
                _audioCalibration.VoiceDetectionThreshold = rms * 3.0; // Set threshold to 3x environmental noise
                _audioCalibration.IsCalibrated = true;
                _audioCalibration.LastCalibrationTime = DateTime.Now;
                
                Debug.WriteLine($"Environmental Noise - RMS: {rms:F6}, dBFS: {dbfsInt}");
            }
        });
    }

    public Task TrainModelWithUserVoiceAsync()
    {
        throw new NotImplementedException();
    }
}