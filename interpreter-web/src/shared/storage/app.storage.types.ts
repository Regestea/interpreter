import type {TranslatorSettings} from "../../features/translator/types/translator.types.ts";

export type AppStorageKeys = 'study' | 'translator' | 'assistance' ;

export type AppStorageSchema = TranslatorSettings | StudySettings | AssistanceSettings;