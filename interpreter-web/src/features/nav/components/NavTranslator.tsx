import {useNavTranslatorStore} from "../../translator/store/translator.store.ts";
import {useNavigate} from "react-router";
import NavButton from "./NavButton.tsx";
import {faGear, faPlay, faRotateBack, faStop} from "@fortawesome/free-solid-svg-icons";

function NavTranslator() {
    const navigate = useNavigate();

    const isStarted = useNavTranslatorStore((s) => s.isStarted);
    const setIsStarted = useNavTranslatorStore((s) => s.setIsStarted);
    const isShowSettings = useNavTranslatorStore((s) => s.isShowSettings);
    const setIsShowSettings = useNavTranslatorStore((s) => s.setIsShowSettings);
    return (
        <>
            {isStarted ? (
                <NavButton onClick={() => setIsStarted(false)} icon={faStop} />
            ) : (
                <>
                    <NavButton onClick={() => navigate("/")} icon={faRotateBack} />
                    <NavButton onClick={()=>setIsShowSettings(!isShowSettings)} icon={faGear} />
                    <NavButton onClick={() => setIsStarted(true)} icon={faPlay} />
                </>
            )}
        </>
    );
}

export default NavTranslator;