import { useState, useEffect } from 'react';
import LiveAudioStream from 'react-native-live-audio-stream';
import { Buffer } from 'buffer';

export const useAudioRMS = (isRecording: boolean) => {
  const [rmsDb, setRmsDb] = useState<number | null>(null);

  useEffect(() => {
    if (isRecording) {
      LiveAudioStream.init({
        sampleRate: 16000,
        channels: 1,
        bitsPerSample: 16,
        audioSource: 6, // VOICE_RECOGNITION
        bufferSize: 4096,
      } as any);

      LiveAudioStream.on('data', (data: string) => {
        const pcmData = Buffer.from(data, 'base64');
        const buffer = pcmData.buffer.slice(pcmData.byteOffset, pcmData.byteOffset + pcmData.byteLength);
        const int16View = new Int16Array(buffer);
        
        let sumSquares = 0;
        for (let i = 0; i < int16View.length; i++) {
          const sample = int16View[i] / 32768;
          sumSquares += sample * sample;
        }
        const rms = Math.sqrt(sumSquares / int16View.length);
        const db = 20 * Math.log10(rms);
        
        // console.log('RMS (dBFS):', db.toFixed(2));
        setRmsDb(db);
      });

      LiveAudioStream.start();
    } else {
      LiveAudioStream.stop();
    }

    return () => {
      LiveAudioStream.stop();
    };
  }, [isRecording]);

  return rmsDb;
};
