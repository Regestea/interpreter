import { useState, useRef, useCallback, useEffect } from 'react';

declare global {
    interface Window {
        webkitAudioContext?: typeof AudioContext;
    }
}

export const DEFAULT_SEGMENTER_CONFIG = {
    chunkDurationMs: 200,
    windowSizeChunks: 15,
    silenceThresholdChunks: 9,
    postRollMs: 500,
    calibrationDurationMs: 2000,
    sensitivityOffset: 5,
};

interface UseSpeechSegmenterOptions {
    chunkDurationMs?: number;
    windowSizeChunks?: number;
    silenceThresholdChunks?: number;
    postRollMs?: number;
    calibrationDurationMs?: number;
    sensitivityOffset?: number;
    onSegmentReady?: (segment: Blob) => void;
}

interface UseSpeechSegmenterReturn {
    isListening: boolean;
    isRecording: boolean;
    isCalibrating: boolean;
    startListening: () => Promise<void>;
    stopListening: () => void;
    error: string | null;
}

export const useSpeechSegmenter = (
    options: UseSpeechSegmenterOptions = {}
): UseSpeechSegmenterReturn => {
    const config = { ...DEFAULT_SEGMENTER_CONFIG, ...options };

    const [isListening, setIsListening] = useState(false);
    const [isRecording, setIsRecording] = useState(false);
    const [isCalibrating, setIsCalibrating] = useState(false);
    const [error, setError] = useState<string | null>(null);

    const audioContextRef = useRef<AudioContext | null>(null);
    const mediaStreamRef = useRef<MediaStream | null>(null);
    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const analyserRef = useRef<AnalyserNode | null>(null);
    const animationFrameIdRef = useRef<number | null>(null);

    const isRecordingRef = useRef(false);
    const audioChunksRef = useRef<Blob[]>([]);
    const lastChunkTimeRef = useRef(0);
    const currentChunkHasSpeechRef = useRef(false);
    const slidingWindowRef = useRef<number[]>([]);
    const stopTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

    const isCalibratingRef = useRef(false);
    const calibrationStartTimeRef = useRef(0);
    const calibrationSamplesRef = useRef<number[]>([]);
    const dynamicThresholdRef = useRef(15);

    const startRecording = useCallback(() => {
        if (mediaRecorderRef.current?.state === 'inactive') {
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
        if (mediaRecorderRef.current?.state === 'recording') {
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

    const processAudio = useCallback(() => {
        const dataArray = new Uint8Array(analyserRef.current?.frequencyBinCount || 0);

        const analyzeFrame = () => {
            if (!analyserRef.current) return;

            analyserRef.current.getByteFrequencyData(dataArray);

            const averageVolume = dataArray.reduce((acc, val) => acc + val, 0) / dataArray.length;
            const currentTime = Date.now();

            if (isCalibratingRef.current) {
                calibrationSamplesRef.current.push(averageVolume);

                if (currentTime - calibrationStartTimeRef.current >= config.calibrationDurationMs) {
                    const noiseFloor = calibrationSamplesRef.current.reduce((acc, val) => acc + val, 0) / calibrationSamplesRef.current.length;
                    dynamicThresholdRef.current = noiseFloor + config.sensitivityOffset;

                    isCalibratingRef.current = false;
                    setIsCalibrating(false);
                    lastChunkTimeRef.current = currentTime;
                }

                animationFrameIdRef.current = requestAnimationFrame(analyzeFrame);
                return;
            }

            if (averageVolume > dynamicThresholdRef.current) {
                currentChunkHasSpeechRef.current = true;

                if (!isRecordingRef.current) {
                    startRecording();
                } else if (stopTimeoutRef.current) {
                    clearTimeout(stopTimeoutRef.current);
                    stopTimeoutRef.current = null;
                }
            }

            if (currentTime - lastChunkTimeRef.current >= config.chunkDurationMs) {
                slidingWindowRef.current.push(currentChunkHasSpeechRef.current ? 1 : 0);

                if (slidingWindowRef.current.length > config.windowSizeChunks) {
                    slidingWindowRef.current.shift();
                }

                if (isRecordingRef.current && !stopTimeoutRef.current) {
                    const silentChunksCount = slidingWindowRef.current.filter(val => val === 0).length;

                    if (slidingWindowRef.current.length === config.windowSizeChunks && silentChunksCount >= config.silenceThresholdChunks) {
                        triggerStopRecording();
                    }
                }

                currentChunkHasSpeechRef.current = false;
                lastChunkTimeRef.current = currentTime;
            }

            animationFrameIdRef.current = requestAnimationFrame(analyzeFrame);
        };

        analyzeFrame();
    }, [config, startRecording, triggerStopRecording]);

    const startListening = useCallback(async () => {
        setError(null);
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaStreamRef.current = stream;

            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (!AudioContextClass) {
                throw new Error('AudioContext not supported');
            }

            const audioContext = new AudioContextClass();
            audioContextRef.current = audioContext;

            const source = audioContext.createMediaStreamSource(stream);
            const analyser = audioContext.createAnalyser();
            analyser.fftSize = 512;
            analyser.smoothingTimeConstant = 0.8;
            source.connect(analyser);
            analyserRef.current = analyser;

            const mediaRecorder = new MediaRecorder(stream, {
                mimeType: MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                    ? 'audio/webm;codecs=opus'
                    : 'audio/webm'
            });
            mediaRecorderRef.current = mediaRecorder;

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunksRef.current.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                const audioBlob = new Blob(audioChunksRef.current, {
                    type: mediaRecorder.mimeType
                });
                options.onSegmentReady?.(audioBlob);
            };

            setIsListening(true);
            isCalibratingRef.current = true;
            setIsCalibrating(true);
            calibrationStartTimeRef.current = Date.now();
            calibrationSamplesRef.current = [];
            currentChunkHasSpeechRef.current = false;
            slidingWindowRef.current = [];

            processAudio();
        } catch (err) {
            const errorMessage = err instanceof Error ? err.message : 'Microphone access denied or an error occurred.';
            setError(errorMessage);
            console.error(err);
        }
    }, [options, processAudio]);

    const stopListening = useCallback(() => {
        if (animationFrameIdRef.current) {
            cancelAnimationFrame(animationFrameIdRef.current);
            animationFrameIdRef.current = null;
        }
        if (stopTimeoutRef.current) {
            clearTimeout(stopTimeoutRef.current);
            stopTimeoutRef.current = null;
        }
        if (isRecordingRef.current) {
            finalizeStopRecording();
        }
        if (mediaStreamRef.current) {
            mediaStreamRef.current.getTracks().forEach(track => track.stop());
            mediaStreamRef.current = null;
        }
        if (audioContextRef.current?.state !== 'closed') {
            audioContextRef.current?.close();
            audioContextRef.current = null;
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
        startListening,
        stopListening,
        error,
    };
};
