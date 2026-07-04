import {useCallback, useEffect, useState} from "react";
import {useSpeechSegmenter} from "./hooks/useSpeechSegmenter.ts";
import {useNavTranslatorStore} from "./store/translator.store.ts";
import type {TranslatorRequest, TranslatorSettingsType} from "./types/translator.types.ts";
import {TranslatorSettings} from "./components/TranslatorSettings.tsx";
import {translateAudio} from "./api/translator.service.ts";
import {appStorage} from "../../shared/storage/app.storage.ts";
import "../../shared/utils/blob-audio-duration.ts";


interface Translator {
    audio: Blob,
    settings: TranslatorSettingsType,
}

function Translator() {

    const localSettings: TranslatorSettingsType = appStorage.getData("translator");
    const [getTranslationList, setTranslationList] = useState<string[]>([]);

    const handleSegmentReady = useCallback(async (segment: Blob) => {
        console.log("handleSegmentReady", segment);
        const duration = await segment.getDurationSeconds();
        console.log("duration", duration);
        if (duration > 2) {

            const request: TranslatorRequest = {
                settings: localSettings,
                audio: segment
            };
            translateAudio(request).then((response) => {
                console.log(response);
                setTranslationList(prev => [...prev, response.translatedText])
            })
        }
    }, []);

    const {
        isListening,
        isRecording,
        isCalibrating,
        startListening,
        stopListening,
        error,
    } = useSpeechSegmenter({
        chunkDurationMs: 100,
        windowSizeChunks: 15,
        silenceThresholdChunks: 6,
        postRollMs: 400,
        calibrationDurationMs: 2000,
        sensitivityOffset: 5,
        onSegmentReady: handleSegmentReady,
    });


    const isStarted = useNavTranslatorStore((s) => s.isStarted);

    useEffect(() => {

        if (isStarted) {
            startListening();
        } else {
            stopListening();
        }
    }, [isStarted, startListening, stopListening])


    return <>

        <div className="d-grid align-content-start overflow-y-auto text-align-left">
            {getTranslationList.map((item, i) => (
                <p className={(getTranslationList.length - 1) == i ? 'clear-text-blue m-0' : 'm-0'} key={i}>
                    {item}
                </p>
            ))}

        </div>

        <TranslatorSettings/>
    </>;
}

export default Translator;
