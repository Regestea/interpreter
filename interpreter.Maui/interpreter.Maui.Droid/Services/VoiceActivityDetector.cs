using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace interpreter.Maui.Services;

public readonly struct SpeechSegment
{
    public TimeSpan Start { get; init; }
    public TimeSpan End { get; init; }
    public TimeSpan Duration => End - Start;
}

public class VadConfiguration
{
    public int FrameMs { get; set; } = 30;
    public int SampleRate { get; set; } = 16000;
    public int AttackFrames { get; set; } = 4;
    public int ReleaseFrames { get; set; } = 25;
    public int PreRollMs { get; set; } = 200;
    public int PostRollMs { get; set; } = 300;
    public float AbsMinRaw { get; set; } = 300f;
    public float StartFactor { get; set; } = 3.0f;
    public float EndFactor { get; set; } = 2.0f;
    public float ThresholdOffset { get; set; } = 0f;
    public float AmbientCalibrationSeconds { get; set; } = 1.0f;
    public long MaxMemoryStorageBytes { get; set; } = 50 * 1024 * 1024;
    public bool UseNormalizedMode { get; set; } = false;

    public int GetFrameSizeSamples() => (SampleRate * FrameMs) / 1000;
    public int GetFrameSizeBytes() => GetFrameSizeSamples() * 2;
    public int GetPreRollSamples() => (SampleRate * PreRollMs) / 1000;
    public int GetPostRollSamples() => (SampleRate * PostRollMs) / 1000;
}

public sealed class VoiceActivityDetector : IDisposable
{
    private readonly VadConfiguration _config;
    private readonly object _lock = new();
    private readonly int _frameSizeSamples;
    private readonly short[] _frameBuffer;
    private int _frameBufferOffset;
    private int _attackCounter;
    private int _releaseCounter;
    private bool _inSpeech;
    private long _totalSamplesProcessed;
    private long _speechStartSample;
    private long _speechEndSample;
    private readonly List<float> _ambientRmsHistory;
    private readonly int _ambientHistorySize;
    private float _ambientNoiseRms;
    private bool _isCalibrated;
    private int _calibrationFramesNeeded;
    private int _calibrationFramesCollected;
    private readonly short[] _preRollBuffer;
    private int _preRollWriteIndex;
    private bool _preRollBufferFilled;
    private MemoryStream? _audioMemoryStream;
    private FileStream? _audioFileStream;
    private string? _tempFilePath;
    private long _storedSamplesStart;
    private bool _useFileStorage;
    private bool _disposed;

    public event Action<TimeSpan>? OnSegmentStarted;
    public event Action<TimeSpan, TimeSpan>? OnSegmentEnded;
    public event Action<SpeechSegment, byte[]>? OnSegmentComplete;

    public float AmbientNoiseRms => _ambientNoiseRms;
    public float StartThreshold => CalculateStartThreshold();
    public float EndThreshold => CalculateEndThreshold();
    public bool IsInSpeech => _inSpeech;
    public bool IsCalibrated => _isCalibrated;
    public long TotalSamplesProcessed => _totalSamplesProcessed;

    public VoiceActivityDetector(VadConfiguration? config = null)
    {
        _config = config ?? new VadConfiguration();
        _frameSizeSamples = _config.GetFrameSizeSamples();
        _frameBuffer = new short[_frameSizeSamples];
        _frameBufferOffset = 0;

        int preRollSamples = _config.GetPreRollSamples();
        _preRollBuffer = new short[preRollSamples];
        _preRollWriteIndex = 0;
        _preRollBufferFilled = false;

        _ambientHistorySize = (int)(_config.AmbientCalibrationSeconds * 1000 / _config.FrameMs);
        _ambientRmsHistory = new List<float>(_ambientHistorySize);
        _calibrationFramesNeeded = _ambientHistorySize;
        _calibrationFramesCollected = 0;
        _ambientNoiseRms = _config.AbsMinRaw;
        _isCalibrated = false;

        _audioMemoryStream = new MemoryStream();
        _storedSamplesStart = 0;
        _useFileStorage = false;
    }

    public void ProcessAudioChunk(short[] samples, int samplesRead, CancellationToken cancellationToken = default)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VoiceActivityDetector));
        if (samples == null) throw new ArgumentNullException(nameof(samples));
        if (samplesRead <= 0) return;

        lock (_lock)
        {
            int offset = 0;
            while (offset < samplesRead && !cancellationToken.IsCancellationRequested)
            {
                int toCopy = Math.Min(samplesRead - offset, _frameSizeSamples - _frameBufferOffset);
                Array.Copy(samples, offset, _frameBuffer, _frameBufferOffset, toCopy);
                _frameBufferOffset += toCopy;
                offset += toCopy;

                if (_frameBufferOffset >= _frameSizeSamples)
                {
                    ProcessFrame(_frameBuffer, _frameSizeSamples);
                    _frameBufferOffset = 0;
                }
            }

            StoreAudio(samples, samplesRead);
            UpdatePreRollBuffer(samples, samplesRead);
            _totalSamplesProcessed += samplesRead;
        }
    }

    public void ProcessAudioChunk(byte[] buffer, int offset, int length, CancellationToken cancellationToken = default)
    {
        if (buffer == null) throw new ArgumentNullException(nameof(buffer));
        if (length <= 0) return;

        int sampleCount = length / 2;
        short[] samples = new short[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            int byteOffset = offset + (i * 2);
            samples[i] = (short)(buffer[byteOffset] | (buffer[byteOffset + 1] << 8));
        }

        ProcessAudioChunk(samples, sampleCount, cancellationToken);
    }

    private void ProcessFrame(short[] frameSamples, int count)
    {
        float rms = CalculateRms(frameSamples, 0, count);
        UpdateAmbientNoise(rms);

        float startThreshold = CalculateStartThreshold();
        float endThreshold = CalculateEndThreshold();

        bool frameAboveStart = rms >= startThreshold;
        bool frameBelowEnd = rms < endThreshold;

        if (!_inSpeech)
        {
            if (frameAboveStart)
            {
                _attackCounter++;
                _releaseCounter = 0;

                if (_attackCounter >= _config.AttackFrames)
                {
                    _inSpeech = true;
                    _attackCounter = 0;

                    long preRollSamples = _config.GetPreRollSamples();
                    _speechStartSample = Math.Max(0, _totalSamplesProcessed - preRollSamples);

                    TimeSpan startTime = SamplesToTimeSpan(_speechStartSample);
                    OnSegmentStarted?.Invoke(startTime);
                }
            }
            else
            {
                _attackCounter = 0;
            }
        }
        else
        {
            if (frameBelowEnd)
            {
                _releaseCounter++;

                if (_releaseCounter >= _config.ReleaseFrames)
                {
                    _inSpeech = false;
                    _releaseCounter = 0;

                    long postRollSamples = _config.GetPostRollSamples();
                    _speechEndSample = _totalSamplesProcessed + postRollSamples;

                    TimeSpan startTime = SamplesToTimeSpan(_speechStartSample);
                    TimeSpan endTime = SamplesToTimeSpan(_speechEndSample);

                    OnSegmentEnded?.Invoke(startTime, endTime);

                    var segment = new SpeechSegment { Start = startTime, End = endTime };
                    byte[]? segmentBytes = GetSegmentBytes(startTime, endTime);
                    if (segmentBytes != null)
                    {
                        OnSegmentComplete?.Invoke(segment, segmentBytes);
                    }
                }
            }
            else
            {
                _releaseCounter = 0;
            }
        }
    }

    private static float CalculateRms(short[] samples, int offset, int count)
    {
        if (count <= 0) return 0f;

        double sumOfSquares = 0.0;
        for (int i = offset; i < offset + count; i++)
        {
            float normalized = samples[i] / 32768f;
            sumOfSquares += normalized * normalized;
        }

        double meanSquare = sumOfSquares / count;
        float rmsNormalized = (float)Math.Sqrt(meanSquare);
        return rmsNormalized * 32768f;
    }

    private void UpdateAmbientNoise(float rms)
    {
        if (!_isCalibrated)
        {
            _ambientRmsHistory.Add(rms);
            _calibrationFramesCollected++;

            if (_calibrationFramesCollected >= _calibrationFramesNeeded)
            {
                _ambientNoiseRms = CalculateMedian(_ambientRmsHistory);
                _isCalibrated = true;
            }
        }
        else if (!_inSpeech)
        {
            float endThreshold = CalculateEndThreshold();
            if (rms < endThreshold)
            {
                _ambientRmsHistory.Add(rms);
                if (_ambientRmsHistory.Count > _ambientHistorySize * 2)
                {
                    _ambientRmsHistory.RemoveAt(0);
                }
                _ambientNoiseRms = CalculateMedian(_ambientRmsHistory);
            }
        }
    }

    private static float CalculateMedian(List<float> values)
    {
        if (values.Count == 0) return 0f;

        var sorted = values.OrderBy(x => x).ToList();
        int mid = sorted.Count / 2;

        if (sorted.Count % 2 == 0)
        {
            return (sorted[mid - 1] + sorted[mid]) / 2f;
        }
        return sorted[mid];
    }

    private float CalculateStartThreshold()
    {
        if (_config.UseNormalizedMode)
        {
            float normalizedAmbient = _ambientNoiseRms / 32768f;
            float threshold = normalizedAmbient * _config.StartFactor + _config.ThresholdOffset;
            return Math.Max(_config.AbsMinRaw / 32768f, threshold) * 32768f;
        }

        float rawThreshold = _ambientNoiseRms * _config.StartFactor + _config.ThresholdOffset;
        return Math.Max(_config.AbsMinRaw, rawThreshold);
    }

    private float CalculateEndThreshold()
    {
        if (_config.UseNormalizedMode)
        {
            float normalizedAmbient = _ambientNoiseRms / 32768f;
            float threshold = normalizedAmbient * _config.EndFactor + _config.ThresholdOffset;
            return Math.Max(_config.AbsMinRaw / 32768f, threshold) * 32768f;
        }

        float rawThreshold = _ambientNoiseRms * _config.EndFactor + _config.ThresholdOffset;
        return Math.Max(_config.AbsMinRaw, rawThreshold);
    }

    private void UpdatePreRollBuffer(short[] samples, int count)
    {
        for (int i = 0; i < count; i++)
        {
            _preRollBuffer[_preRollWriteIndex] = samples[i];
            _preRollWriteIndex = (_preRollWriteIndex + 1) % _preRollBuffer.Length;

            if (_preRollWriteIndex == 0)
            {
                _preRollBufferFilled = true;
            }
        }
    }

    private void StoreAudio(short[] samples, int count)
    {
        byte[] bytes = new byte[count * 2];
        Buffer.BlockCopy(samples, 0, bytes, 0, count * 2);

        if (!_useFileStorage)
        {
            _audioMemoryStream?.Write(bytes, 0, bytes.Length);

            if (_audioMemoryStream != null && _audioMemoryStream.Length >= _config.MaxMemoryStorageBytes)
            {
                SwitchToFileStorage();
            }
        }
        else
        {
            _audioFileStream?.Write(bytes, 0, bytes.Length);
        }
    }

    private void SwitchToFileStorage()
    {
        if (_useFileStorage) return;

        try
        {
            _tempFilePath = Path.Combine(Path.GetTempPath(), $"vad_audio_{Guid.NewGuid()}.raw");
            _audioFileStream = new FileStream(_tempFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);

            if (_audioMemoryStream != null)
            {
                _audioMemoryStream.Position = 0;
                _audioMemoryStream.CopyTo(_audioFileStream);
                _audioMemoryStream.Dispose();
                _audioMemoryStream = null;
            }

            _useFileStorage = true;
        }
        catch
        {
            // Continue with memory storage if file creation fails
        }
    }

    public byte[]? GetSegmentBytes(TimeSpan start, TimeSpan end)
    {
        lock (_lock)
        {
            long startSample = TimeSpanToSamples(start);
            long endSample = TimeSpanToSamples(end);

            startSample = Math.Max(_storedSamplesStart, startSample);
            endSample = Math.Min(_totalSamplesProcessed, endSample);

            if (endSample <= startSample) return null;

            long startByte = (startSample - _storedSamplesStart) * 2;
            long byteCount = (endSample - startSample) * 2;

            byte[] result = new byte[byteCount];

            try
            {
                if (!_useFileStorage && _audioMemoryStream != null)
                {
                    if (startByte < _audioMemoryStream.Length)
                    {
                        _audioMemoryStream.Position = startByte;
                        int read = _audioMemoryStream.Read(result, 0, (int)Math.Min(byteCount, _audioMemoryStream.Length - startByte));
                        if (read < byteCount)
                        {
                            Array.Resize(ref result, read);
                        }
                    }
                }
                else if (_audioFileStream != null)
                {
                    if (startByte < _audioFileStream.Length)
                    {
                        _audioFileStream.Position = startByte;
                        int read = _audioFileStream.Read(result, 0, (int)Math.Min(byteCount, _audioFileStream.Length - startByte));
                        if (read < byteCount)
                        {
                            Array.Resize(ref result, read);
                        }
                    }
                }

                return result;
            }
            catch
            {
                return null;
            }
        }
    }

    public short[] GetPreRollSamples()
    {
        lock (_lock)
        {
            int count = _preRollBufferFilled ? _preRollBuffer.Length : _preRollWriteIndex;
            short[] result = new short[count];

            if (_preRollBufferFilled)
            {
                int firstPart = _preRollBuffer.Length - _preRollWriteIndex;
                Array.Copy(_preRollBuffer, _preRollWriteIndex, result, 0, firstPart);
                Array.Copy(_preRollBuffer, 0, result, firstPart, _preRollWriteIndex);
            }
            else
            {
                Array.Copy(_preRollBuffer, 0, result, 0, count);
            }

            return result;
        }
    }

    private TimeSpan SamplesToTimeSpan(long samples)
    {
        return TimeSpan.FromSeconds((double)samples / _config.SampleRate);
    }

    private long TimeSpanToSamples(TimeSpan time)
    {
        return (long)(time.TotalSeconds * _config.SampleRate);
    }

    public void Reset()
    {
        lock (_lock)
        {
            _frameBufferOffset = 0;
            _attackCounter = 0;
            _releaseCounter = 0;
            _inSpeech = false;
            _totalSamplesProcessed = 0;
            _speechStartSample = 0;
            _speechEndSample = 0;

            _ambientRmsHistory.Clear();
            _isCalibrated = false;
            _calibrationFramesCollected = 0;
            _ambientNoiseRms = _config.AbsMinRaw;

            _preRollWriteIndex = 0;
            _preRollBufferFilled = false;
            Array.Clear(_preRollBuffer, 0, _preRollBuffer.Length);

            _audioMemoryStream?.SetLength(0);
            _audioFileStream?.SetLength(0);
            _storedSamplesStart = 0;
        }
    }

    public void ForceEndSpeech()
    {
        lock (_lock)
        {
            if (_inSpeech)
            {
                _inSpeech = false;
                _speechEndSample = _totalSamplesProcessed;

                TimeSpan startTime = SamplesToTimeSpan(_speechStartSample);
                TimeSpan endTime = SamplesToTimeSpan(_speechEndSample);

                OnSegmentEnded?.Invoke(startTime, endTime);

                var segment = new SpeechSegment { Start = startTime, End = endTime };
                byte[]? segmentBytes = GetSegmentBytes(startTime, endTime);
                if (segmentBytes != null)
                {
                    OnSegmentComplete?.Invoke(segment, segmentBytes);
                }
            }
        }
    }

    public void ClearStorageUpTo(TimeSpan time)
    {
        lock (_lock)
        {
            long clearUpToSample = TimeSpanToSamples(time);
            if (clearUpToSample <= _storedSamplesStart) return;

            long bytesToRemove = (clearUpToSample - _storedSamplesStart) * 2;

            if (!_useFileStorage && _audioMemoryStream != null)
            {
                if (bytesToRemove >= _audioMemoryStream.Length)
                {
                    _audioMemoryStream.SetLength(0);
                }
                else
                {
                    long remaining = _audioMemoryStream.Length - bytesToRemove;
                    byte[] remainingData = new byte[remaining];
                    _audioMemoryStream.Position = bytesToRemove;
                    _audioMemoryStream.Read(remainingData, 0, (int)remaining);
                    _audioMemoryStream.SetLength(0);
                    _audioMemoryStream.Write(remainingData, 0, remainingData.Length);
                }
            }

            _storedSamplesStart = clearUpToSample;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _audioMemoryStream?.Dispose();
        _audioFileStream?.Dispose();

        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            try { File.Delete(_tempFilePath); } catch { /* Cleanup best effort */ }
        }
    }
}

