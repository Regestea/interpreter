import { useState, useEffect, useCallback } from 'react';
import { PermissionsAndroid, Alert } from 'react-native';
import LiveAudioStream from 'react-native-live-audio-stream';
import notifee from '@notifee/react-native';
import { Buffer } from 'buffer';
import '../extensions/buffer.extension';

// Register foreground service
notifee.registerForegroundService((_notification) => {
  return new Promise(() => {
    console.log("Foreground service started");
  });
});

export const useAudioStream = () => {
  const [isRecording, setIsRecording] = useState(false);

  useEffect(() => {
    return () => {
      // Cleanup on unmount
      LiveAudioStream.stop();
      notifee.stopForegroundService().catch(console.error);
    };
  }, []);

  const startStream = useCallback(async () => {
    try {
      console.log('📱 Starting foreground service...');
      
      const granted = await PermissionsAndroid.request(
        PermissionsAndroid.PERMISSIONS.RECORD_AUDIO,
        {
          title: 'Microphone Permission',
          message: 'This app needs access to your microphone to stream audio.',
          buttonNeutral: 'Ask Me Later',
          buttonNegative: 'Cancel',
          buttonPositive: 'OK',
        }
      );
      
      if (granted !== PermissionsAndroid.RESULTS.GRANTED) {
        Alert.alert('Permission Required', 'Please grant microphone permission to access audio.');
        return;
      }

      const options = {
        sampleRate: 16000,
        channels: 1,
        bitsPerSample: 16,
        audioSource: 6,
        bufferSize: 4096,
        wavFile: 'audio.wav'
      };

      LiveAudioStream.init(options);

      LiveAudioStream.on('data', (data: string) => {
        const chunk = Buffer.from(data, 'base64');
        const audioPercentageLevel = chunk.calculateAudioLevel();
        console.log('📊 audio percentage is ', audioPercentageLevel);
      });

      LiveAudioStream.start();

      const channelId = await notifee.createChannel({
        id: "recording",
        name: "Recording",
      });

      await notifee.displayNotification({
        title: "Android audio background streaming",
        body: "streaming microphone...",
        android: {
          channelId,
          asForegroundService: true,
        },
      });

      setIsRecording(true);
      console.log('✅ Foreground service initialized');
    } catch (error) {
      console.error('❌ Failed to start foreground service:', error);
      Alert.alert('Error', 'Failed to start foreground service: ' + error);
    }
  }, []);

  const stopStream = useCallback(async () => {
    try {
      console.log('⏹️ Stopping foreground service...');
      LiveAudioStream.stop();
      await notifee.stopForegroundService();
      setIsRecording(false);
      console.log('🛑 Foreground service stopped');
    } catch (error) {
      console.error('❌ Failed to stop foreground service:', error);
      Alert.alert('Error', 'Failed to stop foreground service: ' + error);
    }
  }, []);

  const toggleRecording = useCallback(() => {
    if (isRecording) {
      stopStream();
    } else {
      startStream();
    }
  }, [isRecording, startStream, stopStream]);

  return {
    isRecording,
    toggleRecording
  };
};
