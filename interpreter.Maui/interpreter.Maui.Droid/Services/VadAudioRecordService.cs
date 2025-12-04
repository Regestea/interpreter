using System.Threading;
using System.Threading.Tasks;
using Android.Media;
using Android.Util;

namespace interpreter.Maui.Services;

public sealed class VadAudioRecordService : IDisposable
{
    private const string Tag = "VadAudioRecordService";

    private readonly VadConfiguration _vadConfig;
    private VoiceActivityDetector? _vad;
    private AudioRecord? _audioRecord;
    private Thread? _recordingThread;
    private CancellationTokenSource? _cts;
    private volatile bool _isRecording;

    private readonly int _sampleRate;
    private readonly ChannelIn _channelConfig;
    private readonly Encoding _audioFormat;
    private readonly int _minBufferSize;

    public event Action<TimeSpan>? OnSpeechStarted;
    public event Action<TimeSpan, TimeSpan>? OnSpeechEnded;
    public event Action<SpeechSegment, byte[]>? OnSpeechSegmentReady;

    public VadAudioRecordService(VadConfiguration? vadConfig = null)
    {
        _vadConfig = vadConfig ?? new VadConfiguration();
        _sampleRate = _vadConfig.SampleRate;
        _channelConfig = ChannelIn.Mono;
        _audioFormat = Encoding.Pcm16bit;

        _minBufferSize = AudioRecord.GetMinBufferSize(_sampleRate, _channelConfig, _audioFormat);
        if (_minBufferSize <= 0)
        {
            _minBufferSize = _sampleRate * 2;
        }
    }

    public bool IsRecording => _isRecording;
    public VoiceActivityDetector? Vad => _vad;

    public void StartRecording()
    {
        if (_isRecording) return;

        _vad = new VoiceActivityDetector(_vadConfig);
        _vad.OnSegmentStarted += start => OnSpeechStarted?.Invoke(start);
        _vad.OnSegmentEnded += (start, end) => OnSpeechEnded?.Invoke(start, end);
        _vad.OnSegmentComplete += (segment, bytes) => OnSpeechSegmentReady?.Invoke(segment, bytes);

        _cts = new CancellationTokenSource();

#pragma warning disable CA1416
        _audioRecord = new AudioRecord(
            AudioSource.Mic,
            _sampleRate,
            _channelConfig,
            _audioFormat,
            _minBufferSize * 2);

        if (_audioRecord.State != State.Initialized)
        {
            Log.Error(Tag, "Failed to initialize AudioRecord");
            _audioRecord.Dispose();
            _audioRecord = null;
            return;
        }

        _audioRecord.StartRecording();
#pragma warning restore CA1416

        _isRecording = true;

        _recordingThread = new Thread(() => RecordingLoop(_cts.Token))
        {
            IsBackground = true,
            Name = "VadAudioRecordThread",
            Priority = ThreadPriority.Highest
        };
        _recordingThread.Start();

        Log.Info(Tag, $"Started recording: {_sampleRate}Hz, Buffer: {_minBufferSize} bytes");
    }

    public void StopRecording()
    {
        if (!_isRecording) return;

        _isRecording = false;
        _cts?.Cancel();

        _recordingThread?.Join(1000);
        _recordingThread = null;

#pragma warning disable CA1416
        if (_audioRecord != null)
        {
            try { _audioRecord.Stop(); } catch { /* Best effort cleanup */ }
            _audioRecord.Release();
            _audioRecord.Dispose();
            _audioRecord = null;
        }
#pragma warning restore CA1416

        _vad?.ForceEndSpeech();

        Log.Info(Tag, "Stopped recording");
    }

    private void RecordingLoop(CancellationToken cancellationToken)
    {
        int bufferSizeSamples = _minBufferSize / 2;
        short[] buffer = new short[bufferSizeSamples];

        try
        {
            while (_isRecording && !cancellationToken.IsCancellationRequested)
            {
#pragma warning disable CA1416
                int samplesRead = _audioRecord?.Read(buffer, 0, bufferSizeSamples) ?? 0;
#pragma warning restore CA1416

                if (samplesRead > 0)
                {
                    _vad?.ProcessAudioChunk(buffer, samplesRead, cancellationToken);
                }
                else if (samplesRead < 0)
                {
                    Log.Warn(Tag, $"AudioRecord.Read returned error: {samplesRead}");
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when stopping
        }
        catch (Exception ex)
        {
            Log.Error(Tag, $"Recording loop error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        StopRecording();
        _vad?.Dispose();
        _cts?.Dispose();
    }
}

public static class VadWavHelper
{
    public static byte[] CreateWavFile(byte[] pcmData, int sampleRate, int channels = 1, int bitsPerSample = 16)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        int byteRate = sampleRate * channels * (bitsPerSample / 8);
        short blockAlign = (short)(channels * (bitsPerSample / 8));

        writer.Write(['R', 'I', 'F', 'F']);
        writer.Write(36 + pcmData.Length);
        writer.Write(['W', 'A', 'V', 'E']);

        writer.Write(['f', 'm', 't', ' ']);
        writer.Write(16);
        writer.Write((short)1);
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write(blockAlign);
        writer.Write((short)bitsPerSample);

        writer.Write(['d', 'a', 't', 'a']);
        writer.Write(pcmData.Length);
        writer.Write(pcmData);

        return ms.ToArray();
    }

    public static MemoryStream CreateWavStream(byte[] pcmData, int sampleRate, int channels = 1, int bitsPerSample = 16)
    {
        byte[] wavData = CreateWavFile(pcmData, sampleRate, channels, bitsPerSample);
        return new MemoryStream(wavData);
    }
}

