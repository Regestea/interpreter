import { useState, useRef, useCallback, useEffect } from 'react';

interface UseSpeechSegmenterOptions {
    chunkDurationMs?: number;      // طول هر قطعه: 200 میلی‌ثانیه
    windowSizeChunks?: number;     // تعداد کل قطعات پنجره لغزان: 15
    silenceThresholdChunks?: number; // حداقل تعداد 0ها برای قطع صدا: 9
    volumeThreshold?: number;      // حد آستانه حجم صدا برای تشخیص 1 (بسته به میکروفون قابل تنظیم است)
    postRollMs?: number;           // زمان اضافه در انتهای ضبط برای جلوگیری از بریده شدن انتهای صدا
}

interface UseSpeechSegmenterReturn {
    isListening: boolean;
    isRecording: boolean;
    audioSegments: Blob[];
    startListening: () => Promise<void>;
    stopListening: () => void;
    error: string | null;
}

export const useSpeechSegmenter = ({
                                       chunkDurationMs = 200,
                                       windowSizeChunks = 15,
                                       silenceThresholdChunks = 9,
                                       volumeThreshold = 15,
                                       postRollMs = 500, // 500 میلی‌ثانیه ضبط اضافه برای کامل افتادن کلمه آخر
                                   }: UseSpeechSegmenterOptions = {}): UseSpeechSegmenterReturn => {
    const [isListening, setIsListening] = useState<boolean>(false);
    const [isRecording, setIsRecording] = useState<boolean>(false);
    const [audioSegments, setAudioSegments] = useState<Blob[]>([]);
    const [error, setError] = useState<string | null>(null);

    const audioContextRef = useRef<AudioContext | null>(null);
    const mediaStreamRef = useRef<MediaStream | null>(null);
    const mediaRecorderRef = useRef<MediaRecorder | null>(null);
    const analyserRef = useRef<AnalyserNode | null>(null);
    const animationFrameIdRef = useRef<number | null>(null);

    const isRecordingRef = useRef<boolean>(false);
    const audioChunksRef = useRef<Blob[]>([]);

    // Refs مربوط به Sliding Window
    const lastChunkTimeRef = useRef<number>(0);
    const currentChunkHasSpeechRef = useRef<boolean>(false);
    const slidingWindowRef = useRef<number[]>([]); // حاوی 0 (سکوت) و 1 (صدا)
    const stopTimeoutRef = useRef<NodeJS.Timeout | null>(null);

    const startRecording = useCallback(() => {
        if (mediaRecorderRef.current && mediaRecorderRef.current.state === 'inactive') {
            audioChunksRef.current = [];
            mediaRecorderRef.current.start();
            isRecordingRef.current = true;
            setIsRecording(true);

            // پاکسازی پنجره هنگام شروع یک دیالوگ جدید برای جلوگیری از تداخل
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
            slidingWindowRef.current = []; // ریست کردن پنجره برای دیالوگ بعدی
        }
    }, []);

    const triggerStopRecording = useCallback(() => {
        // جلوگیری از اجرای چندباره تایمر توقف
        if (stopTimeoutRef.current) return;

        // اعمال Post-roll برای جلوگیری از بریده شدن انتهای صدا
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

        // اگر در این فریم (کسری از ثانیه) صدا از آستانه بالاتر بود، این قطعه 200 میلی‌ثانیه‌ای برابر 1 خواهد شد
        if (averageVolume > volumeThreshold) {
            currentChunkHasSpeechRef.current = true;

            // اگر در حال ضبط نیستیم، فورا ضبط را شروع کن (جلوگیری از بریده شدن ابتدای صدا)
            if (!isRecordingRef.current) {
                startRecording();
            } else if (stopTimeoutRef.current) {
                // اگر تایمر توقف روشن شده بود ولی کاربر دوباره صحبت کرد، توقف را لغو کن
                clearTimeout(stopTimeoutRef.current);
                stopTimeoutRef.current = null;
            }
        }

        // بررسی پایان بازه 200 میلی‌ثانیه‌ای (Chunk)
        if (currentTime - lastChunkTimeRef.current >= chunkDurationMs) {
            // 1 برای صدا، 0 برای سکوت
            const chunkStatus = currentChunkHasSpeechRef.current ? 1 : 0;

            // اضافه کردن وضعیت به انتهای پنجره لغزان
            slidingWindowRef.current.push(chunkStatus);

            // حفظ طول پنجره روی حداکثر 15 قطعه
            if (slidingWindowRef.current.length > windowSizeChunks) {
                slidingWindowRef.current.shift(); // حذف قدیمی‌ترین قطعه از ابتدای آرایه
            }

            // اگر در حال ضبط هستیم، پنجره را برای یافتن سکوت بررسی می‌کنیم
            if (isRecordingRef.current && !stopTimeoutRef.current) {
                // شمارش تعداد 0ها در آرایه فعلی
                const silentChunksCount = slidingWindowRef.current.filter((val) => val === 0).length;

                // اگر پنجره پر شده (15 تایی است) و تعداد 0ها حداقل 9 تا است
                if (
                    slidingWindowRef.current.length === windowSizeChunks &&
                    silentChunksCount >= silenceThresholdChunks
                ) {
                    triggerStopRecording();
                }
            }

            // ریست کردن متغیرها برای پردازش قطعه 200 میلی‌ثانیه‌ای بعدی
            currentChunkHasSpeechRef.current = false;
            lastChunkTimeRef.current = currentTime;
        }

        // ادامه چرخه بررسی صدا
        animationFrameIdRef.current = requestAnimationFrame(processAudio);
    }, [
        chunkDurationMs,
        windowSizeChunks,
        silenceThresholdChunks,
        volumeThreshold,
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

            // مقداردهی اولیه برای شروع حلقه
            lastChunkTimeRef.current = Date.now();
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
    }, [finalizeStopRecording]);

    useEffect(() => {
        return () => stopListening();
    }, [stopListening]);

    return {
        isListening,
        isRecording,
        audioSegments,
        startListening,
        stopListening,
        error,
    };
};
