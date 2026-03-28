import { useState, useRef, useCallback, useEffect } from 'react';

// Make webkitAudioContext available globally to prevent TypeScript 'any' errors
declare global {
    interface Window {
        webkitAudioContext?: typeof AudioContext;
    }
}

// ============================================================================
// CORE CONFIGURATION
// Modify these default values to fine-tune Voice Activity Detection (VAD)
// ============================================================================
export const DEFAULT_SEGMENTER_CONFIG = {
    chunkDurationMs: 200,          // Duration of a single sliding window chunk
    windowSizeChunks: 15,          // Total chunks in the window (e.g., 15 * 200ms = 3000ms)
    silenceThresholdChunks: 9,     // Minimum silent chunks needed to trigger recording stop
    postRollMs: 500,               // Delay before stopping to prevent cutting off the last word
    calibrationDurationMs: 2000,   // Initial duration to measure the ambient noise floor
    sensitivityOffset: 5,          // Buffer added to the noise floor to determine speech threshold
};

interface UseSpeechSegmenterOptions {
    chunkDurationMs?: number;
    windowSizeChunks?: number;
    silenceThresholdChunks?: number;
    postRollMs?: number;
    calibrationDurationMs?: number;
    sensitivityOffset?: number;
}

interface UseSpeechSegmenterReturn {
    isListening: boolean;
    isRecording: boolean;
    isCalibrating: boolean;
    audioSegments: Blob[];
    startListening: () => Promise<void>;
    stopListening: () => void;
    error: string | null;
}

export const useSpeechSegmenter = (
    options: UseSpeechSegmenterOptions = {}
): UseSpeechSegmenterReturn => {
    // Merge user options with default configurations
    const config = { ...DEFAULT_SEGMENTER_CONFIG, ...options };

    // --------------------------------------------------------------------------
    // States
    // --------------------------------------------------------------------------
    const [isListening, setIsListening] = useState<boolean>(false);
    const [isRecording, setIsRecording] = useState<boolean>(false);
    const [isCalibrating, setIsCalibrating] = useState<boolean>(false);
    const [audioSegments, setAudioSegments] = useState<Blob[]>([]);
    const [error, setError] = useState<string | null>(null);

    // --------------------------------------------------------------------------
    // Media & Audio Node Refs
    // --------------------------------------------------------------------------
    const audioContextRef = useRef<AudioContext | null>(null);
    const mediaStreamRef = useRef<MediaStream | null>(null);
    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const analyserRef = useRef<AnalyserNode | null>(null);
    const animationFrameIdRef = useRef<number | null>(null);

    // --------------------------------------------------------------------------
    // Recording & VAD Refs
    // --------------------------------------------------------------------------
    const isRecordingRef = useRef<boolean>(false);
    const audioChunksRef = useRef<Blob[]>([]);
    const lastChunkTimeRef = useRef<number>(0);
    const currentChunkHasSpeechRef = useRef<boolean>(false);
    const slidingWindowRef = useRef<number[]>([]);
    const stopTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    // --------------------------------------------------------------------------
    // Calibration Refs
    // --------------------------------------------------------------------------
    const isCalibratingRef = useRef<boolean>(false);
    const calibrationStartTimeRef = useRef<number>(0);
    const calibrationSamplesRef = useRef<number[]>([]);
    const dynamicThresholdRef = useRef<number>(15); // Fallback threshold

    // ============================================================================
    // Recording Controllers
    // ============================================================================

    const startRecording = useCallback(() => {
        if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'inactive') {
            audioChunksRef.current = [];
            mediaRecorderRef.current.start();
            isRecordingRef.current = true;
            setIsRecording(true);
            slidingWindowRef.current = [];

            if (stopTimeoutRef.current) {
                clearTimeout(stopTimeoutRef.current);
                stopTimeoutRef.current = null;
            }
        }
    }, []);

    const finalizeStopRecording = useCallback(() => {
        if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'recording') {
            mediaRecorderRef.current.stop();
            isRecordingRef.current = false;
            setIsRecording(false);
            slidingWindowRef.current = [];
        }
    }, []);

    const triggerStopRecording = useCallback(() => {
        if (stopTimeoutRef.current) return;

        stopTimeoutRef.current = setTimeout(() => {
            finalizeStopRecording();
            stopTimeoutRef.current = null;
        }, config.postRollMs);
    }, [finalizeStopRecording, config.postRollMs]);

    // ============================================================================
    // Core Audio Processing Loop
    // ============================================================================

    const processAudio = useCallback(() => {
        const analyzeFrame = () => {
            if (!analyserRef.current) return;

            const bufferLength = analyserRef.current.frequencyBinCount;
            const dataArray = new Uint8Array(bufferLength);
            analyserRef.current.getByteFrequencyData(dataArray);

            const sum = dataArray.reduce((acc, val) => acc + val, 0);
            const averageVolume = sum / bufferLength;
            const currentTime = Date.now();

            // --- Phase 1: Environmental Noise Calibration ---
            if (isCalibratingRef.current) {
                calibrationSamplesRef.current.push(averageVolume);

                if (currentTime - calibrationStartTimeRef.current >= config.calibrationDurationMs) {
                    const noiseSum = calibrationSamplesRef.current.reduce((acc, val) => acc + val, 0);
                    const noiseFloor = noiseSum / calibrationSamplesRef.current.length;

                    // Set dynamic threshold based on calculated noise floor
                    dynamicThresholdRef.current = noiseFloor + config.sensitivityOffset;

                    isCalibratingRef.current = false;
                    setIsCalibrating(false);
                    lastChunkTimeRef.current = currentTime;
                }

                animationFrameIdRef.current = requestAnimationFrame(analyzeFrame);
                return; // Skip VAD processing until calibration is complete
            }

            // --- Phase 2: Voice Activity Detection (VAD) ---
            if (averageVolume > dynamicThresholdRef.current) {
                currentChunkHasSpeechRef.current = true;

                if (!isRecordingRef.current) {
                    startRecording();
                } else if (stopTimeoutRef.current) {
                    clearTimeout(stopTimeoutRef.current);
                    stopTimeoutRef.current = null;
                }
            }

            // --- Phase 3: Sliding Window Processing ---
            if (currentTime - lastChunkTimeRef.current >= config.chunkDurationMs) {
                const chunkStatus = currentChunkHasSpeechRef.current ? 1 : 0;
                slidingWindowRef.current.push(chunkStatus);

                // Maintain the fixed window size
                if (slidingWindowRef.current.length > config.windowSizeChunks) {
                    slidingWindowRef.current.shift();
                }

                // Evaluate stop condition based on silence threshold
                if (isRecordingRef.current && !stopTimeoutRef.current) {
                    const silentChunksCount = slidingWindowRef.current.filter((val) => val === 0).length;

                    if (
                        slidingWindowRef.current.length === config.windowSizeChunks &&
                        silentChunksCount >= config.silenceThresholdChunks
                    ) {
                        triggerStopRecording();
                    }
                }

                // Reset for the next chunk
                currentChunkHasSpeechRef.current = false;
                lastChunkTimeRef.current = currentTime;
            }

            // Queue next frame
            animationFrameIdRef.current = requestAnimationFrame(analyzeFrame);
        };

        analyzeFrame();
    }, [
        config.chunkDurationMs,
        config.windowSizeChunks,
        config.silenceThresholdChunks,
        config.calibrationDurationMs,
        config.sensitivityOffset,
        startRecording,
        triggerStopRecording,
    ]);

    // ============================================================================
    // Initialization & Cleanup
    // ============================================================================

    const startListening = async () => {
        setError(null);
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaStreamRef.current = stream;

            // Cross-browser AudioContext initialization
            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            const audioContext = new AudioContextClass!();
            audioContextRef.current = audioContext;

            const source = audioContext.createMediaStreamSource(stream);
            const analyser = audioContext.createAnalyser();
            analyser.fftSize = 512;
            source.connect(analyser);
            analyserRef.current = analyser;

            const mediaRecorder = new MediaRecorder(stream);
            mediaRecorderRef.current = mediaRecorder;

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunksRef.current.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                const audioBlob = new Blob(audioChunksRef.current, { type: 'audio/webm' });
                setAudioSegments((prev) => [...prev, audioBlob]);
            };

            setIsListening(true);

            // Initialize Calibration State
            isCalibratingRef.current = true;
            setIsCalibrating(true);
            calibrationStartTimeRef.current = Date.now();
            calibrationSamplesRef.current = [];

            // Initialize VAD State
            currentChunkHasSpeechRef.current = false;
            slidingWindowRef.current = [];

            processAudio();
        } catch (err) {
            setError('Microphone access denied or an error occurred.');
            console.error(err);
        }
    };

    const stopListening = useCallback(() => {
        if (animationFrameIdRef.current) {
            cancelAnimationFrame(animationFrameIdRef.current);
        }
        if (stopTimeoutRef.current) {
            clearTimeout(stopTimeoutRef.current);
        }
        if (isRecordingRef.current) {
            finalizeStopRecording();
        }
        if (mediaStreamRef.current) {
            mediaStreamRef.current.getTracks().forEach((track) => track.stop());
        }
        if (audioContextRef.current) {
            audioContextRef.current.close();
        }

        setIsListening(false);
        setIsCalibrating(false);
        isCalibratingRef.current = false;
    }, [finalizeStopRecording]);

    useEffect(() => {
        return () => stopListening();
    }, [stopListening]);

    return {
        isListening,
        isRecording,
        isCalibrating,
        audioSegments,
        startListening,
        stopListening,
        error,
    };
};
