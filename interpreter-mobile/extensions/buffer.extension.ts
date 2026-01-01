import { Buffer } from 'buffer';

const MIN_DB = -80;
const MAX_DB = -10;

declare module 'buffer' {
  export interface Buffer {
    calculateAudioLevel(): number | null;
  }
}

Buffer.prototype.calculateAudioLevel = function() {
  if (this.length < 2) return null;

  let sumSquares = 0;
  const samplesCount = Math.floor(this.length / 2);

  // Iterate by 2 bytes (16-bit)
  for (let i = 0; i < samplesCount * 2; i += 2) {
      // Read 16-bit signed integer, Little Endian
      // This ensures we are interpreting the bytes correctly regardless of system endianness
      const sample = this.readInt16LE(i);
      sumSquares += sample * sample;
  }

  const meanSquare = sumSquares / samplesCount;
  const rms = Math.sqrt(meanSquare);

  // dBFS = 20 * log10(RMS / MaxAmplitude)
  // MaxAmplitude for 16-bit is 32768
  const normalizedRms = rms / 32768.0;

  // Clamp to avoid -Infinity
  const db = normalizedRms > 1e-9 ? 20 * Math.log10(normalizedRms) : -160;

  // Map dB to 0-100 range
  let percentage = ((db - MIN_DB) / (MAX_DB - MIN_DB)) * 100;
  percentage = Math.max(0, Math.min(100, percentage));

  return percentage;
};
