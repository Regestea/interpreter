import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View, Button, Alert } from 'react-native';
import { useEffect, useRef, useState } from 'react';
import notifee from '@notifee/react-native';
import { Audio, InterruptionModeIOS } from 'expo-av';

// Register foreground service outside of component
notifee.registerForegroundService((notification) => {
  return new Promise(() => {
    console.log("Foreground service started");
  });
});

export default function App() {
  const [isRecording, setIsRecording] = useState(false);
  const recordingRef = useRef<Audio.Recording | null>(null);

  useEffect(() => {
    // Cleanup on unmount
    return () => {
      if (recordingRef.current) {
        recordingRef.current.stopAndUnloadAsync().catch(console.error);
      }
      notifee.stopForegroundService().catch(console.error);
    };
  }, []);

  const startForegroundService = async () => {
    try {
      console.log('📱 Starting foreground service...');
      
      // Request audio permissions
      const permission = await Audio.requestPermissionsAsync();

      if (!permission.granted) {
        Alert.alert('Permission Required', 'Please grant microphone permission to record audio.');
        return;
      }

      // Set audio mode for recording
      await Audio.setAudioModeAsync({
        allowsRecordingIOS: true,
        playsInSilentModeIOS: true,
        staysActiveInBackground: true,
        interruptionModeIOS: InterruptionModeIOS.DuckOthers,
      });

      // Create recording instance
      const { recording } = await Audio.Recording.createAsync(
        Audio.RecordingOptionsPresets.HIGH_QUALITY
      );

      recordingRef.current = recording;

      // Create notification channel
      const channelId = await notifee.createChannel({
        id: "recording",
        name: "Recording",
      });

      // Display notification as foreground service
      await notifee.displayNotification({
        title: "Android audio background recording",
        body: "recording...",
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
      
      // Stop and unload recording
      if (recordingRef.current) {
        await recordingRef.current.stopAndUnloadAsync();
        recordingRef.current = null;
        console.log('🎙️ Recording stopped and unloaded');
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