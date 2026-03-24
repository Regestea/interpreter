import { create } from "zustand/react";

interface NavTranslatorState {
    isStarted: boolean;
    isShowSettings: boolean;

    setIsStarted: (started: boolean) => void;
    setIsShowSettings: (show: boolean) => void;
}

export const useNavTranslatorStore = create<NavTranslatorState>((set) => ({
    isStarted: false,
    isShowSettings: false,

    setIsStarted: (started) => set({ isStarted: started }),
    setIsShowSettings: (show) => set({ isShowSettings: show }),
}));
