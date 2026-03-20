import { create } from "zustand"
import type { CurrentNav } from "../types/nav.types"

interface CurrentNavState {
    currentNav: CurrentNav
    setCurrentNav: (currentNav: CurrentNav) => void
}

export const useNavStore = create<CurrentNavState>((set) => ({
    currentNav: "home",
    setCurrentNav: (currentNav) => set({ currentNav })
}))
