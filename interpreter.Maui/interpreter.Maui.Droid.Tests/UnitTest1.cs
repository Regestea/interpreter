using interpreter.Maui.Services;

namespace interpreter.Maui.Droid.Tests;

/// <summary>
/// Unit tests for VoiceActivityDetector.
/// </summary>
public class VoiceActivityDetectorTests
{
    /// <summary>
    /// Creates test samples with a specific RMS level.
    /// </summary>
    private static short[] CreateSamplesWithRms(float targetRms, int count)
    {
        short amplitude = (short)Math.Min(targetRms, short.MaxValue);
        short[] samples = new short[count];
        for (int i = 0; i < count; i++)
        {
            samples[i] = (i % 2 == 0) ? amplitude : (short)-amplitude;
        }
        return samples;
    }

    /// <summary>
    /// Creates silent samples (near-zero RMS).
    /// </summary>
    private static short[] CreateSilentSamples(int count)
    {
        return new short[count];
    }

    [Fact]
    public void ProcessAudioChunk_WithSilence_DoesNotTriggerSpeech()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30,
            AttackFrames = 4,
            ReleaseFrames = 10,
            AbsMinRaw = 300f
        };

        using var vad = new VoiceActivityDetector(config);

        bool speechStarted = false;
        vad.OnSegmentStarted += _ => speechStarted = true;

        var silentSamples = CreateSilentSamples(16000 * 2);
        vad.ProcessAudioChunk(silentSamples, silentSamples.Length);

        Assert.False(speechStarted);
        Assert.False(vad.IsInSpeech);
    }

    [Fact]
    public void ProcessAudioChunk_AttackFrames_RequiresConsecutiveFrames()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30,
            AttackFrames = 4,
            ReleaseFrames = 10,
            AbsMinRaw = 300f,
            StartFactor = 2.0f
        };

        using var vad = new VoiceActivityDetector(config);

        var calibrationSamples = CreateSamplesWithRms(200f, 16000);
        vad.ProcessAudioChunk(calibrationSamples, calibrationSamples.Length);

        Assert.True(vad.IsCalibrated);

        bool speechStarted = false;
        vad.OnSegmentStarted += _ => speechStarted = true;

        int frameSamples = config.GetFrameSizeSamples();
        float highRms = config.AbsMinRaw * 4;

        for (int i = 0; i < 3; i++)
        {
            var highSamples = CreateSamplesWithRms(highRms, frameSamples);
            vad.ProcessAudioChunk(highSamples, highSamples.Length);
        }

        Assert.False(speechStarted);

        var triggerSamples = CreateSamplesWithRms(highRms, frameSamples);
        vad.ProcessAudioChunk(triggerSamples, triggerSamples.Length);

        Assert.True(speechStarted);
    }

    [Fact]
    public void ProcessAudioChunk_ReleaseFrames_RequiresConsecutiveFrames()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30,
            AttackFrames = 2,
            ReleaseFrames = 5,
            AbsMinRaw = 300f,
            StartFactor = 2.0f,
            EndFactor = 1.5f
        };

        using var vad = new VoiceActivityDetector(config);

        var calibrationSamples = CreateSamplesWithRms(200f, 16000);
        vad.ProcessAudioChunk(calibrationSamples, calibrationSamples.Length);

        bool speechEnded = false;
        vad.OnSegmentEnded += (_, _) => speechEnded = true;

        int frameSamples = config.GetFrameSizeSamples();
        float highRms = config.AbsMinRaw * 4;

        for (int i = 0; i < 5; i++)
        {
            vad.ProcessAudioChunk(CreateSamplesWithRms(highRms, frameSamples), frameSamples);
        }

        Assert.True(vad.IsInSpeech);

        for (int i = 0; i < 4; i++)
        {
            vad.ProcessAudioChunk(CreateSilentSamples(frameSamples), frameSamples);
        }

        Assert.False(speechEnded);

        vad.ProcessAudioChunk(CreateSilentSamples(frameSamples), frameSamples);

        Assert.True(speechEnded);
    }

    [Fact]
    public void TransientSpike_DoesNotTriggerFalseSpeech()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30,
            AttackFrames = 4,
            AbsMinRaw = 300f
        };

        using var vad = new VoiceActivityDetector(config);

        var calibrationSamples = CreateSamplesWithRms(200f, 16000);
        vad.ProcessAudioChunk(calibrationSamples, calibrationSamples.Length);

        bool speechStarted = false;
        vad.OnSegmentStarted += _ => speechStarted = true;

        int frameSamples = config.GetFrameSizeSamples();

        vad.ProcessAudioChunk(CreateSamplesWithRms(5000f, frameSamples), frameSamples);
        vad.ProcessAudioChunk(CreateSilentSamples(frameSamples), frameSamples);

        Assert.False(speechStarted);
    }

    [Fact]
    public void AdaptiveThreshold_UpdatesBasedOnAmbientNoise()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30,
            AmbientCalibrationSeconds = 0.5f,
            AbsMinRaw = 100f,
            StartFactor = 3.0f
        };

        using var vad = new VoiceActivityDetector(config);

        float calibrationRms = 500f;
        int calibrationSamples = (int)(16000 * 0.6f);
        var samples = CreateSamplesWithRms(calibrationRms, calibrationSamples);
        vad.ProcessAudioChunk(samples, samples.Length);

        Assert.True(vad.IsCalibrated);
        Assert.True(vad.AmbientNoiseRms > 300f && vad.AmbientNoiseRms < 700f);
    }

    [Fact]
    public void GetSegmentBytes_ReturnsCorrectSlice()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30
        };

        using var vad = new VoiceActivityDetector(config);

        var samples = new short[16000];
        for (int i = 0; i < samples.Length; i++)
        {
            samples[i] = (short)(i % 1000);
        }

        vad.ProcessAudioChunk(samples, samples.Length);

        var segmentBytes = vad.GetSegmentBytes(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.75));

        Assert.NotNull(segmentBytes);
        Assert.Equal(16000, segmentBytes!.Length);
    }

    [Fact]
    public void ForceEndSpeech_EndsOngoingSpeech()
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 30,
            AttackFrames = 2,
            ReleaseFrames = 100,
            AbsMinRaw = 300f
        };

        using var vad = new VoiceActivityDetector(config);

        vad.ProcessAudioChunk(CreateSamplesWithRms(200f, 16000), 16000);

        bool speechEnded = false;
        vad.OnSegmentEnded += (_, _) => speechEnded = true;

        int frameSamples = config.GetFrameSizeSamples();
        for (int i = 0; i < 5; i++)
        {
            vad.ProcessAudioChunk(CreateSamplesWithRms(2000f, frameSamples), frameSamples);
        }

        Assert.True(vad.IsInSpeech);
        Assert.False(speechEnded);

        vad.ForceEndSpeech();

        Assert.False(vad.IsInSpeech);
        Assert.True(speechEnded);
    }

    [Fact]
    public void FrameSizeCalculations_AreCorrect()
    {
        var config16k = new VadConfiguration { SampleRate = 16000, FrameMs = 30 };
        Assert.Equal(480, config16k.GetFrameSizeSamples());
        Assert.Equal(960, config16k.GetFrameSizeBytes());

        var config48k = new VadConfiguration { SampleRate = 48000, FrameMs = 30 };
        Assert.Equal(1440, config48k.GetFrameSizeSamples());
        Assert.Equal(2880, config48k.GetFrameSizeBytes());
    }

    [Theory]
    [InlineData(100, 200)]
    [InlineData(50, 100)]
    [InlineData(300, 500)]
    public void ShortWordDetection_Works(int speechDurationMs, int postSilenceMs)
    {
        var config = new VadConfiguration
        {
            SampleRate = 16000,
            FrameMs = 20,
            AttackFrames = 2,
            ReleaseFrames = 5,
            AbsMinRaw = 300f
        };

        using var vad = new VoiceActivityDetector(config);

        vad.ProcessAudioChunk(CreateSamplesWithRms(200f, 16000), 16000);

        bool speechStarted = false;
        bool speechEnded = false;
        vad.OnSegmentStarted += _ => speechStarted = true;
        vad.OnSegmentEnded += (_, _) => speechEnded = true;

        int frameSamples = config.GetFrameSizeSamples();
        int speechFrames = (speechDurationMs / config.FrameMs) + config.AttackFrames;
        int silenceFrames = (postSilenceMs / config.FrameMs) + config.ReleaseFrames;

        for (int i = 0; i < speechFrames; i++)
        {
            vad.ProcessAudioChunk(CreateSamplesWithRms(2000f, frameSamples), frameSamples);
        }

        Assert.True(speechStarted);

        for (int i = 0; i < silenceFrames; i++)
        {
            vad.ProcessAudioChunk(CreateSilentSamples(frameSamples), frameSamples);
        }

        Assert.True(speechEnded);
    }
}