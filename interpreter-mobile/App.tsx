import { StatusBar } from 'expo-status-bar';
import { StyleSheet, Text, View, Button } from 'react-native';
import { useAudioStream } from './hooks/useAudioStream';

export default function App() {
  const { isRecording, toggleRecording } = useAudioStream();

  return (
    <View style={styles.container}>
      <Text style={styles.title}>Interpreter Mobile App</Text>
      <Text style={styles.status}>
        Status: {isRecording ? '🔴 Recording' : '⚫ Idle'}
      </Text>
      <View style={styles.buttonContainer}>
        <Button 
          title={isRecording ? "Stop Recording" : "Start Recording"} 
          onPress={toggleRecording}
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