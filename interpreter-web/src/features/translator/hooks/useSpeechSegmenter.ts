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
    // Destructuring با مقادیر پیش‌فرض برای جلوگیری از رندرهای اضافی
    const {
        chunkDurationMs = DEFAULT_SEGMENTER_CONFIG.chunkDurationMs,
        windowSizeChunks = DEFAULT_SEGMENTER_CONFIG.windowSizeChunks,
        silenceThresholdChunks = DEFAULT_SEGMENTER_CONFIG.silenceThresholdChunks,
        postRollMs = DEFAULT_SEGMENTER_CONFIG.postRollMs,
        calibrationDurationMs = DEFAULT_SEGMENTER_CONFIG.calibrationDurationMs,
        sensitivityOffset = DEFAULT_SEGMENTER_CONFIG.sensitivityOffset,
        onSegmentReady
    } = options;

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
        }, postRollMs);
    }, [finalizeStopRecording, postRollMs]);

    const processAudio = useCallback(() => {
        const dataArray = new Uint8Array(analyserRef.current?.frequencyBinCount || 0);

        const analyzeFrame = () => {
            if (!analyserRef.current) return;

            analyserRef.current.getByteFrequencyData(dataArray);

            // محاسبه بهینه میانگین صدا با حلقه for
            let sum = 0;
            const length = dataArray.length;
            for (let i = 0; i < length; i++) {
                sum += dataArray[i];
            }
            const averageVolume = sum / length;
            const currentTime = Date.now();

            if (isCalibratingRef.current) {
                calibrationSamplesRef.current.push(averageVolume);

                if (currentTime - calibrationStartTimeRef.current >= calibrationDurationMs) {
                    let calSum = 0;
                    for (let i = 0; i < calibrationSamplesRef.current.length; i++) {
                        calSum += calibrationSamplesRef.current[i];
                    }
                    const noiseFloor = calSum / calibrationSamplesRef.current.length;

                    // فرمول آستانه نویز: $Threshold = NoiseFloor + Offset$
                    dynamicThresholdRef.current = noiseFloor + sensitivityOffset;

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

            if (currentTime - lastChunkTimeRef.current >= chunkDurationMs) {
                slidingWindowRef.current.push(currentChunkHasSpeechRef.current ? 1 : 0);

                if (slidingWindowRef.current.length > windowSizeChunks) {
                    slidingWindowRef.current.shift();
                }

                if (isRecordingRef.current && !stopTimeoutRef.current) {
                    let silentChunksCount = 0;
                    for (let i = 0; i < slidingWindowRef.current.length; i++) {
                        if (slidingWindowRef.current[i] === 0) silentChunksCount++;
                    }

                    if (slidingWindowRef.current.length === windowSizeChunks && silentChunksCount >= silenceThresholdChunks) {
                        triggerStopRecording();
                    }
                }

                currentChunkHasSpeechRef.current = false;
                lastChunkTimeRef.current = currentTime;
            }

            animationFrameIdRef.current = requestAnimationFrame(analyzeFrame);
        };

        analyzeFrame();
    }, [
        calibrationDurationMs,
        chunkDurationMs,
        sensitivityOffset,
        silenceThresholdChunks,
        windowSizeChunks,
        startRecording,
        triggerStopRecording
    ]);

    const startListening = useCallback(async () => {
        // Guard Clause برای جلوگیری از اجرای همزمان و نشت حافظه
        if (isListening || mediaStreamRef.current) return;

        setError(null);
        try {
            // ۱. بهینه‌سازی Constraints برای Whisper
            const audioConstraints: MediaTrackConstraints = {
                sampleRate: 16000,      // $16 kHz$
                channelCount: 1,        // Mono ($1$ Channel)
                echoCancellation: true,
                noiseSuppression: true,
                autoGainControl: true
            };

            const stream = await navigator.mediaDevices.getUserMedia({ audio: audioConstraints });
            mediaStreamRef.current = stream;

            const AudioContextClass = window.AudioContext || window.webkitAudioContext;
            if (!AudioContextClass) {
                setError('AudioContext not supported in this browser');
                return;
            }

            // ۲. همگام‌سازی AudioContext با نرخ نمونه‌برداری Whisper
            const audioContext = new AudioContextClass({ sampleRate: 16000 });
            audioContextRef.current = audioContext;

            const source = audioContext.createMediaStreamSource(stream);
            const analyser = audioContext.createAnalyser();
            analyser.fftSize = 512;
            analyser.smoothingTimeConstant = 0.8;
            source.connect(analyser);
            analyserRef.current = analyser;

            // ۳. انتخاب هوشمندانه فرمت برای پشتیبانی کراس‌پلتفرم (مخصوصاً Safari)
            let optionsMimeType: string | undefined = undefined;
            if (MediaRecorder.isTypeSupported('audio/webm;codecs=opus')) {
                optionsMimeType = 'audio/webm;codecs=opus';
            } else if (MediaRecorder.isTypeSupported('audio/webm')) {
                optionsMimeType = 'audio/webm';
            } else if (MediaRecorder.isTypeSupported('audio/mp4')) {
                optionsMimeType = 'audio/mp4';
            }

            // ۴. تنظیم Bitrate بهینه برای کلام
            const mediaRecorderOptions: MediaRecorderOptions = {
                audioBitsPerSecond: 128000, // $128 kbps$
            };

            if (optionsMimeType) {
                mediaRecorderOptions.mimeType = optionsMimeType;
            }

            const mediaRecorder = new MediaRecorder(stream, mediaRecorderOptions);
            mediaRecorderRef.current = mediaRecorder;

            mediaRecorder.ondataavailable = (event) => {
                if (event.data.size > 0) {
                    audioChunksRef.current.push(event.data);
                }
            };

            mediaRecorder.onstop = () => {
                const actualMimeType = mediaRecorder.mimeType || 'audio/webm';
                const audioBlob = new Blob(audioChunksRef.current, {
                    type: actualMimeType
                });
                if (onSegmentReady) {
                    onSegmentReady(audioBlob);
                }
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
    }, [isListening, onSegmentReady, processAudio]);

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
        if (audioContextRef.current && audioContextRef.current.state !== 'closed') {
            audioContextRef.current.close();
            audioContextRef.current = null;
        }

        setIsListening(false);
        setIsCalibrating(false);
        isCalibratingRef.current = false;
    }, [finalizeStopRecording]);

    // پاکسازی کامل هنگام Unmount شدن کامپوننت
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
