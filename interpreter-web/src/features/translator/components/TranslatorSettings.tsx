import {useNavTranslatorStore} from "../store/translator.store.ts";
import {appStorage} from "../../../shared/storage/app.storage.ts";
import {OutputLanguage} from "../../../shared/enums/outputLanguage.enum.ts";
import {InputLanguage} from "../../../shared/enums/inputLanguage.enum.ts";
import {useForm} from "react-hook-form";
import BaseModal from "../../../shared/components/BaseModal.tsx";
import type {TranslatorSettings} from "../types/translator.types.ts";


export function TranslatorSettings() {

    const isShowSettings = useNavTranslatorStore((s) => s.isShowSettings);
    const setIsShowSettings = useNavTranslatorStore((s) => s.setIsShowSettings);

    const closeModal = () => setIsShowSettings(false);

    let settings:TranslatorSettings | null = appStorage.getData("translator");
    if (settings == null){
        settings={
            inputLanguage: InputLanguage.English,
            outputLanguage: OutputLanguage.Persian,
            audioRead: false,
            ignoreMyTalks: false,
            voiceTune: "goodvoice"
        };
        appStorage.setData("translator",settings);
    }

    const {
        register,
        handleSubmit
    } = useForm<TranslatorSettings>({
        defaultValues: settings
    });

    const onSubmit = (values:TranslatorSettings) => {
        console.log(values);
        appStorage.setData("translator",values);
        closeModal();
    }
    
    
    return <>

        <BaseModal
            show={isShowSettings}
            onClose={closeModal}
            title="Settings"
            size="sm"
        >
            <form onSubmit={handleSubmit(onSubmit)}>

                <div className="mb-3">
                    <label className="form-label">Input Language</label>
                    <select className="form-select" {...register("inputLanguage")}>
                        <option value="en">English</option>
                        <option value="fa">Persian</option>
                    </select>
                </div>

                <div className="mb-3">
                    <label className="form-label">Output Language</label>
                    <select className="form-select" {...register("outputLanguage")}>
                        <option value="en">English</option>
                        <option value="fa">Persian</option>
                    </select>
                </div>

                <div className="form-check mb-3">
                    <input
                        type="checkbox"
                        className="form-check-input"
                        id="audioRead"
                        {...register("audioRead")}
                    />
                    <label className="form-check-label" htmlFor="audioRead">
                        Read Audio
                    </label>
                </div>

                <div className="form-check mb-3">
                    <input
                        type="checkbox"
                        className="form-check-input"
                        id="ignoreMyTalks"
                        {...register("ignoreMyTalks")}
                    />
                    <label className="form-check-label" htmlFor="ignoreMyTalks">
                        Ignore My Talks
                    </label>
                </div>

                <div className="mb-3">
                    <label className="form-label">Voice Tune</label>
                    <select className="form-select" {...register("voiceTune")}>
                        <option value="goodvoice">Good Voice</option>
                        <option value="verygoodvoice">Very Good Voice</option>
                    </select>
                </div>

                <div className="d-flex align-items-center">
                    <button type="submit" className="btn glass-button">
                        Save
                    </button>

                    <button
                        type="button"
                        onClick={closeModal} // استفاده از closeModal برای بستن
                        className="btn glass-button glass-button-primary ms-auto"
                    >
                        Close
                    </button>
                </div>

            </form>

        </BaseModal>
    </>;
}

