import NavButton from "./NavButton.tsx";
import {faLanguage,faLandmarkFlag, faPlay, faRotateBack,faVolumeHigh} from "@fortawesome/free-solid-svg-icons";
import {useNavStore} from "../store/currentNavStore.ts";
import {useNavigate} from "react-router";

type NavLiveTranslationProps = {};

function NavLiveTranslation() {
    const setCurrentNav = useNavStore((state) => state.setCurrentNav);
    const navigate = useNavigate();
    
    return <>
        <NavButton onClick={() => {
            setCurrentNav("home");
            navigate("/");
        }} icon={faRotateBack}/>
        <NavButton icon={faLanguage}/>
        <NavButton icon={faLandmarkFlag}/>
        <NavButton icon={faVolumeHigh}/>
        <NavButton icon={faPlay}/>
        <NavButton icon={"Lang"}/>
    </>;
}

export default NavLiveTranslation;