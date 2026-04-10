import {InputLanguage} from "../../../shared/enums/inputLanguage.enum.ts";
import {OutputLanguage} from "../../../shared/enums/outputLanguage.enum.ts";


export type TranslatorSettingsType = {
    inputLanguage: InputLanguage,
    outputLanguage: OutputLanguage,
    audioRead: boolean,
    ignoreMyTalks: boolean,
    voiceTune: "goodvoice" | "verygoodvoice",
}

export const DEFAULT_TRANSLATOR_SETTINGS: TranslatorSettingsType = {
    inputLanguage: InputLanguage.English,
    outputLanguage: OutputLanguage.Persian,
    audioRead: false,
    ignoreMyTalks: false,
    voiceTune: "goodvoice",
};


export type TranslatorRequest = {
    settings: TranslatorSettingsType,
    audio:Blob
}

export type TranslatorResponse = {
    language:string,
    audioText:string,
    translatedText:string
}