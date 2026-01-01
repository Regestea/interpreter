import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View, Button, Alert } from 'react-native';
import { useEffect, useRef, useState } from 'react';

export default function App() {
  const [isRecording, setIsRecording] = useState(false);
  const intervalRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    // Cleanup on unmount
    return () => {
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
      }
    };
  }, []);

  const startForegroundService = () => {
    try {
      console.log('📱 Starting foreground service...');
      console.log('✅ Foreground service initialized');
      console.log('🚀 Foreground service is now running');
      
      // Simulate foreground service logging every 2 seconds
      let counter = 0;
      intervalRef.current = setInterval(() => {
        counter++;
        console.log(`🎙️ [Foreground Service] Running... (${counter}s)`);
        console.log(`📊 [Foreground Service] Processing data chunk #${counter}`);
      }, 2000);
      
      setIsRecording(true);
      console.log('▶️ Service started successfully');
    } catch (error) {
      console.error('❌ Failed to start foreground service:', error);
      Alert.alert('Error', 'Failed to start foreground service: ' + error);
    }
  };

  const stopForegroundService = () => {
    try {
      console.log('⏹️ Stopping foreground service...');
      
      // Stop the interval timer
      if (intervalRef.current) {
        clearInterval(intervalRef.current);
        intervalRef.current = null;
        console.log('⏱️ Service timer cleared');
      }
      
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