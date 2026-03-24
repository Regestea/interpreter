import type {InputLanguage} from "../../../shared/enums/inputLanguage.enum.ts";
import type {OutputLanguage} from "../../../shared/enums/outputLanguage.enum.ts";

export type TranslatorSettings = {
    inputLanguage: InputLanguage,
    outputLanguage: OutputLanguage,
    audioRead: boolean,
    ignoreMyTalks: boolean,
    voiceTune: "goodvoice" | "verygoodvoice",
}