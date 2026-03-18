import { useState, useEffect, useCallback, useRef } from 'react';
import { PermissionsAndroid, Alert, Platform } from 'react-native';
import LiveAudioStream from 'react-native-live-audio-stream'; // Keep for native
import notifee from '@notifee/react-native';
import { Buffer } from 'buffer';
import '../extensions/buffer.extension'; // Assuming this extension is correctly implemented

// --- Platform Check ---
const isWeb = Platform.OS === 'web';
const isAndroid = Platform.OS === 'android';

// --- Web Audio API related variables ---
let mediaStream: MediaStream | null = null;
let audioContext: AudioContext | null = null;
let mediaStreamSource: MediaStreamAudioSourceNode | null = null;
let analyser: AnalyserNode | null = null;

// --- Function to handle microphone permissions ---
const requestMicrophonePermission = async (): Promise<boolean> => {
  if (isWeb) {
    try {
      // Request permission for web
      mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true });
      return true;
    } catch (error) {
      console.error('Web microphone permission denied or error:', error);
      Alert.alert('Permission Required', 'Please grant microphone access in your browser settings.');
      return false;
    }
  } else if (isAndroid) {
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
    return granted === PermissionsAndroid.RESULTS.GRANTED;
  } else { // iOS
    console.log('iOS microphone permission is handled by the system.');
    // For iOS native, PermissionsAndroid won't work. If this hook is used in a React Native iOS app,
    // you'd typically use a library like 'react-native-permissions' or handle it differently.
    // For now, we assume permission is granted or handled by system prompts for native iOS.
    return true;
  }
};

// --- Foreground Service Setup (Android Only) ---
const setupForegroundService = async (): Promise<void> => {
  if (isAndroid) { // Only execute on Android
    try {
      const channelId = await notifee.createChannel({
        id: "recording",
        name: "Recording",
      });

      await notifee.displayNotification({
        title: "Audio background streaming",
        body: "Microphone is active.",
        android: {
          channelId,
          asForegroundService: true,
        },
      });
      console.log('📱 Foreground service initialized');
    } catch (error: any) {
      console.error('❌ Failed to initialize foreground service:', error);
      throw error;
    }
  }
};

const stopForegroundService = async (): Promise<void> => {
  if (isAndroid) { // Only execute on Android
    try {
      await notifee.stopForegroundService();
      console.log('🛑 Foreground service stopped');
    } catch (error: any) {
      console.error('❌ Failed to stop foreground service:', error);
    }
  }
};

// --- Web Audio Processing Setup ---
const setupWebAudio = () => {
  if (!audioContext) {
    audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
    analyser = audioContext.createAnalyser();
    analyser.fftSize = 2048; // Sets the resolution for frequency analysis
  }
  if (mediaStream && !mediaStreamSource) {
    mediaStreamSource = audioContext!.createMediaStreamSource(mediaStream);
    mediaStreamSource.connect(analyser!);
    analyser!.connect(audioContext!.destination); // Connect analyser to output (optional, but good for debugging)
    console.log('✅ Web Audio API setup complete.');
  }
};

const processWebAudio = () => {
  if (!analyser || !mediaStream) return;

  const bufferLength = analyser.frequencyBinCount;
  const dataArray = new Uint8Array(bufferLength);
  analyser.getByteFrequencyData(dataArray);

  // Example: Calculate average volume (simple approach)
  let sum = 0;
  for (let i = 0; i < bufferLength; i++) {
    sum += dataArray[i];
  }
  const average = sum / bufferLength;
  const audioPercentageLevel = (average / 255) * 100; // Normalize to 0-100%

  // Here you would typically send this `audioPercentageLevel` or processed audio data
  // to your backend, or use it for something in the UI.
  // For demonstration, we log it.
  console.log('📊 Web Audio Level: ', audioPercentageLevel.toFixed(2) + '%');

  // Schedule the next analysis
  requestAnimationFrame(processWebAudio);
};


// --- Main Hook ---
export const useAudioStream = () => {
  const [isRecording, setIsRecording] = useState<boolean>(false);
  const isWebRef = useRef(isWeb); // Use ref to access isWeb inside callbacks without re-creating them

  // --- Cleanup Function ---
  const cleanup = useCallback(async () => {
    console.log('🧹 Running cleanup...');
    if (!isWebRef.current) {
      // Native cleanup
      try {
        LiveAudioStream.stop();
        console.log('🛑 LiveAudioStream stopped (Native)');
      } catch (error: any) {
        console.error('❌ Failed to stop LiveAudioStream (Native):', error);
      }
    } else {
      // Web cleanup
      if (analyser) analyser.disconnect();
      if (mediaStreamSource) mediaStreamSource.disconnect();
      if (audioContext && audioContext.state !== 'closed') {
        await audioContext.close();
        console.log('🛑 Web AudioContext closed');
      }
      if (mediaStream) {
        mediaStream.getTracks().forEach(track => track.stop());
        console.log('🛑 Web MediaStream stopped');
      }
      // Reset web audio variables
      audioContext = null;
      mediaStreamSource = null;
      analyser = null;
      mediaStream = null;
    }
    await stopForegroundService(); // This function is already conditional for Android
    setIsRecording(false);
    console.log('✅ Cleanup complete.');
  }, []); // Empty dependency array, cleanup logic is self-contained


  useEffect(() => {
    // Effect for component mount/unmount
    return cleanup; // Return the cleanup function to be called on unmount
  }, [cleanup]); // Ensure cleanup is stable


  // --- Start Stream Logic ---
  const startStream = useCallback(async () => {
    console.log('Attempting to start stream...');
    const hasPermission = await requestMicrophonePermission();
    if (!hasPermission) {
      console.log('Microphone permission not granted.');
      return;
    }

    // Setup foreground service only if on Android
    if (isAndroid) {
      try {
        await setupForegroundService();
      } catch (error) {
        console.error("Failed to setup foreground service, cannot proceed.", error);
        return; // Stop if foreground service setup fails on Android
      }
    }

    setIsRecording(true); // Set recording state early

    if (!isWebRef.current) {
      // --- NATIVE PLATFORM (Android/iOS) ---
      try {
        const options = {
          sampleRate: 16000,
          channels: 1,
          bitsPerSample: 16,
          audioSource: 6, // MediaRecorder.AudioSource.VOICE_RECOGNITION on Android
          bufferSize: 4096,
          // wavFile: 'audio.wav' // Note: Consider if this is needed or how it's handled.
        };

        LiveAudioStream.init(options);

        LiveAudioStream.on('data', (data: string) => {
          try {
            const chunk = Buffer.from(data, 'base64');
            // Ensure 'calculateAudioLevel' is correctly extended on the Buffer prototype
            const audioPercentageLevel = chunk.calculateAudioLevel();
            console.log('📊 Native Audio Level: ', audioPercentageLevel.toFixed(2) + '%');
            // If you need to process audio data further, do it here.
            // For example, send `chunk` to a backend.
          } catch (e: any) {
            console.error("Native - Error processing audio data chunk:", e);
          }
        });

        LiveAudioStream.start();
        console.log('✅ Native audio stream started');

      } catch (error: any) {
        console.error('❌ Failed to start native audio stream:', error);
        Alert.alert('Error', `Failed to start native audio stream: ${error.message || error}`);
        await cleanup(); // Attempt cleanup on error
      }
    } else {
      // --- WEB PLATFORM ---
      console.log('Web platform: Setting up Web Audio API...');
      try {
        setupWebAudio(); // Initialize AudioContext, AnalyserNode, etc.
        if (mediaStream && audioContext && analyser) {
          // Start the audio processing loop
          requestAnimationFrame(processWebAudio);
          console.log('✅ Web audio processing initiated');
        } else {
          throw new Error("Failed to initialize Web Audio components.");
        }
      } catch (error: any) {
        console.error('❌ Failed to setup Web Audio API:', error);
        Alert.alert('Error', `Failed to setup web audio: ${error.message || error}`);
        await cleanup(); // Attempt cleanup on error
      }
    }
  }, [isAndroid, cleanup]); // Add dependencies that are used inside


  // --- Toggle Recording Logic ---
  const toggleRecording = useCallback(() => {
    if (isRecording) {
      console.log('Toggling off...');
      cleanup(); // Use cleanup for stopping/tearing down
    } else {
      console.log('Toggling on...');
      startStream();
    }
  }, [isRecording, startStream, cleanup]);

  return {
    isRecording,
    toggleRecording
  };
};
