import {
    DEFAULT_TRANSLATOR_SETTINGS,
    type TranslatorSettingsType
} from "../../features/translator/types/translator.types.ts";
import {appStorage} from "../storage/app.storage.ts";

export class InitializeSettingsService {
    static run(): void {
        this.ensureTranslatorSettings();
    }

    private static ensureTranslatorSettings(): void {
        const settings = appStorage.getData("translator") as TranslatorSettingsType | null;

        if (settings == null) {
            appStorage.setData("translator", DEFAULT_TRANSLATOR_SETTINGS);
        }
    }
}