import type {TranslatorRequest, TranslatorResponse} from "../types/translator.types.ts";
import {axiosInstance} from "../../../lib/axios.ts";

export const translateAudio = async (
    payload: TranslatorRequest
): Promise<TranslatorResponse> => {
    const formData = new FormData();

    formData.append("audio", payload.audio);
    formData.append("settings", JSON.stringify(payload.settings));

    const { data } = await axiosInstance.post<TranslatorResponse>(
        "/translate",
        formData
    );

    return data;
};
