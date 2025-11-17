﻿namespace Opus.Services;

public interface IOpusCodecService
{
    /// <summary>
    /// Encodes PCM WAV Stream into OPUS format (24 kbps).
    /// Supports any input WAV format - automatically resamples to 16 kHz mono.
    /// </summary>
    /// <param name="wavStream">Input WAV stream (any format)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encoded OPUS stream with length-prefixed frames</returns>
    Task<Stream> EncodeAsync(Stream wavStream, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decodes OPUS Stream into 16 kHz mono WAV Stream
    /// </summary>
    /// <param name="opusStream">OPUS stream with length-prefixed frames</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Decoded 16 kHz mono 16-bit PCM WAV stream</returns>
    Task<Stream> DecodeAsync(Stream opusStream, CancellationToken cancellationToken = default);
}
