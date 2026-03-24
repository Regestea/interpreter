import type {AppStorageKeys, AppStorageSchema} from "./app.storage.types.ts";

export const appStorage = {
    setData(key: AppStorageKeys, value: AppStorageSchema): void {
        localStorage.setItem(key, JSON.stringify(value));
    },

    getData(key: AppStorageKeys): AppStorageSchema | null {
        const item = localStorage.getItem(key);
        return item ? JSON.parse(item) : null;
    },

    removeData(key: AppStorageKeys): void {
        localStorage.removeItem(key);
    }
};
