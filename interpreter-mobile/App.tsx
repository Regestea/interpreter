import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View, Button, Alert, PermissionsAndroid, Platform } from 'react-native';
import { useEffect, useState } from 'react';
import notifee from '@notifee/react-native';
import LiveAudioStream from 'react-native-live-audio-stream';

// Register foreground service outside of component
notifee.registerForegroundService((_notification) => {
  return new Promise(() => {
    console.log("Foreground service started");
  });
});

export default function App() {
  const [isRecording, setIsRecording] = useState(false);
  const [isStreaming, setIsStreaming] = useState(false);

  useEffect(() => {
    // Cleanup on unmount
    return () => {
      if (isStreaming) {
        LiveAudioStream.stop();
      }
      notifee.stopForegroundService().catch(console.error);
    };
  }, [isStreaming]);

  const startForegroundService = async () => {
    try {
      console.log('📱 Starting foreground service...');
      
      // Request audio permissions for Android
      if (Platform.OS === 'android') {
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
      }

      // Configure audio stream
      const options = {
        sampleRate: 16000,  // 16kHz sample rate
        channels: 1,        // Mono
        bitsPerSample: 16,  // 16-bit
        audioSource: 6,     // VOICE_RECOGNITION
        bufferSize: 4096,   // Buffer size
        wavFile: 'audio.wav' // Required by library but won't be used for streaming
      };

      LiveAudioStream.init(options);

      // Set up audio data listener
      LiveAudioStream.on('data', (data: string) => {
        // This is where you'll receive audio chunks as base64 strings
        // You can process the audio data here
        console.log('📊 Received audio chunk, length:', data.length);
        // TODO: Process audio chunk (you'll implement this later)
      });

      // Start streaming
      LiveAudioStream.start();
      setIsStreaming(true);

      // Create notification channel
      const channelId = await notifee.createChannel({
        id: "recording",
        name: "Recording",
      });

      // Display notification as foreground service
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
      console.log('🚀 Foreground service is now running');
      console.log('▶️ Service started successfully');
    } catch (error) {
      console.error('❌ Failed to start foreground service:', error);
      Alert.alert('Error', 'Failed to start foreground service: ' + error);
    }
  };

  const stopForegroundService = async () => {
    try {
      console.log('⏹️ Stopping foreground service...');
      
      // Stop audio streaming
      if (isStreaming) {
        LiveAudioStream.stop();
        setIsStreaming(false);
        console.log('🎙️ Audio streaming stopped');
      }
      
      // Stop foreground service
      await notifee.stopForegroundService();
      console.log('🛑 Foreground service stopped');
      
      setIsRecording(false);
      console.log('📱 Service state updated to idle');
    } catch (error) {
      console.error('❌ Failed to stop foreground service:', error);
      Alert.alert('Error', 'Failed to stop foreground service: ' + error);
    }
  };

  const handleToggleRecording = () => {
    if (isRecording) {
      stopForegroundService();
    } else {
      startForegroundService();
    }
  };

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Interpreter Mobile App</Text>
      <Text style={styles.status}>
        Status: {isRecording ? '🔴 Recording' : '⚫ Idle'}
      </Text>
      <View style={styles.buttonContainer}>
        <Button 
          title={isRecording ? "Stop Recording" : "Start Recording"} 
          onPress={handleToggleRecording}
          color={isRecording ? "#ff4444" : "#007bff"}
        />
      </View>
      <StatusBar style="auto" />
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: '#fff',
    alignItems: 'center',
    justifyContent: 'center',
  },
  title: {
    fontSize: 24,
    fontWeight: 'bold',
    marginBottom: 20,
  },
  status: {
    fontSize: 18,
    marginBottom: 40,
    color: '#666',
  },
  buttonContainer: {
    width: '80%',
  },
});