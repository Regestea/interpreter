import { useState, useRef, useCallback, useEffect } from 'react';

interface UseSpeechSegmenterOptions {
    chunkDurationMs?: number;      // طول هر قطعه: 200 میلی‌ثانیه
    windowSizeChunks?: number;     // تعداد کل قطعات پنجره لغزان: 15
    silenceThresholdChunks?: number; // حداقل تعداد 0ها برای قطع صدا: 9
    postRollMs?: number;           // زمان اضافه در انتهای ضبط برای جلوگیری از بریده شدن
    calibrationDurationMs?: number; // زمان کالیبراسیون اولیه برای تشخیص نویز محیط (مثلا 2000 میلی‌ثانیه)
    sensitivityOffset?: number;    // حاشیه حساسیت بالاتر از کف نویز برای تشخیص صحبت (مثلا 5 واحد)
}

interface UseSpeechSegmenterReturn {
    isListening: boolean;
    isRecording: boolean;
    isCalibrating: boolean;        // وضعیت جدید برای نمایش در UI حین کالیبره کردن
    audioSegments: Blob[];
    startListening: () => Promise<void>;
    stopListening: () => void;
    error: string | null;
}

export const useSpeechSegmenter = ({
                                       chunkDurationMs = 200,
                                       windowSizeChunks = 15,
                                       silenceThresholdChunks = 9,
                                       postRollMs = 500,
                                       calibrationDurationMs = 2000,
                                       sensitivityOffset = 5,
                                   }: UseSpeechSegmenterOptions = {}): UseSpeechSegmenterReturn => {
    const [isListening, setIsListening] = useState<boolean>(false);
    const [isRecording, setIsRecording] = useState<boolean>(false);
    const [isCalibrating, setIsCalibrating] = useState<boolean>(false);
    const [audioSegments, setAudioSegments] = useState<Blob[]>([]);
    const [error, setError] = useState<string | null>(null);

    const audioContextRef = useRef<AudioContext | null>(null);
    const mediaStreamRef = useRef<MediaStream | null>(null);
    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const analyserRef = useRef<AnalyserNode | null>(null);
    const animationFrameIdRef = useRef<number | null>(null);

    const isRecordingRef = useRef<boolean>(false);
    const audioChunksRef = useRef<Blob[]>([]);

    // Sliding Window & VAD Refs
    const lastChunkTimeRef = useRef<number>(0);
    const currentChunkHasSpeechRef = useRef<boolean>(false);
    const slidingWindowRef = useRef<number[]>([]);
    const stopTimeoutRef = useRef<NodeJS.Timeout | null>(null);

    // Calibration Refs
    const isCalibratingRef = useRef<boolean>(false);
    const calibrationStartTimeRef = useRef<number>(0);
    const calibrationSamplesRef = useRef<number[]>([]);
    const dynamicThresholdRef = useRef<number>(15); // مقدار پیش‌فرض که بعد از کالیبره آپدیت می‌شود

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
        }, postRollMs);
    }, [finalizeStopRecording, postRollMs]);

    const processAudio = useCallback(() => {
        if (!analyserRef.current) return;

        const bufferLength = analyserRef.current.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        analyserRef.current.getByteFrequencyData(dataArray);

        const sum = dataArray.reduce((a, b) => a + b, 0);
        const averageVolume = sum / bufferLength;
        const currentTime = Date.now();

        // ----------------------------------------------------
        // فاز کالیبراسیون (اندازه‌گیری کف نویز)
        // ----------------------------------------------------
        if (isCalibratingRef.current) {
            calibrationSamplesRef.current.push(averageVolume);

            // اگر زمان کالیبراسیون تمام شد
            if (currentTime - calibrationStartTimeRef.current >= calibrationDurationMs) {
                // محاسبه میانگین نویز محیط
                const noiseSum = calibrationSamplesRef.current.reduce((a, b) => a + b, 0);
                const noiseFloor = noiseSum / calibrationSamplesRef.current.length;

                // تنظیم حد آستانه دینامیک بر اساس نویز محیط + حساسیت
                dynamicThresholdRef.current = noiseFloor + sensitivityOffset;

                isCalibratingRef.current = false;
                setIsCalibrating(false);
                lastChunkTimeRef.current = currentTime; // شروع زمان‌بندی قطعات بعد از کالیبره
            }

            animationFrameIdRef.current = requestAnimationFrame(processAudio);
            return; // خروج از تابع تا زمانی که کالیبراسیون تمام شود
        }

        // ----------------------------------------------------
        // فاز تشخیص صحبت (بر اساس آستانه دینامیک محاسبه شده)
        // ----------------------------------------------------
        if (averageVolume > dynamicThresholdRef.current) {
            currentChunkHasSpeechRef.current = true;

            if (!isRecordingRef.current) {
                startRecording();
            } else if (stopTimeoutRef.current) {
                clearTimeout(stopTimeoutRef.current);
                stopTimeoutRef.current = null;
            }
        }

        // پردازش پنجره لغزان
        if (currentTime - lastChunkTimeRef.current >= chunkDurationMs) {
            const chunkStatus = currentChunkHasSpeechRef.current ? 1 : 0;
            slidingWindowRef.current.push(chunkStatus);

            if (slidingWindowRef.current.length > windowSizeChunks) {
                slidingWindowRef.current.shift();
            }

            if (isRecordingRef.current && !stopTimeoutRef.current) {
                const silentChunksCount = slidingWindowRef.current.filter((val) => val === 0).length;

                if (
                    slidingWindowRef.current.length === windowSizeChunks &&
                    silentChunksCount >= silenceThresholdChunks
                ) {
                    triggerStopRecording();
                }
            }

            currentChunkHasSpeechRef.current = false;
            lastChunkTimeRef.current = currentTime;
        }

        animationFrameIdRef.current = requestAnimationFrame(processAudio);
    }, [
        chunkDurationMs,
        windowSizeChunks,
        silenceThresholdChunks,
        calibrationDurationMs,
        sensitivityOffset,
        startRecording,
        triggerStopRecording
    ]);

    const startListening = async () => {
        setError(null);
        try {
            const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
            mediaStreamRef.current = stream;

            const audioContext = new (window.AudioContext || (window as any).webkitAudioContext)();
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

            // مقداردهی اولیه برای فاز کالیبراسیون
            isCalibratingRef.current = true;
            setIsCalibrating(true);
            calibrationStartTimeRef.current = Date.now();
            calibrationSamplesRef.current = [];

            // مقداردهی اولیه برای حلقه اصلی
            currentChunkHasSpeechRef.current = false;
            slidingWindowRef.current = [];

            processAudio();
        } catch (err) {
            setError('دسترسی به میکروفون رد شد یا خطایی رخ داد.');
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
