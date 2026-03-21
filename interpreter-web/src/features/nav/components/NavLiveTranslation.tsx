import NavButton from "./NavButton.tsx";
import {faLanguage,faLandmarkFlag, faPlay, faRotateBack,faVolumeHigh} from "@fortawesome/free-solid-svg-icons";
import {useNavigate} from "react-router";

function NavLiveTranslation() {
    const navigate = useNavigate();
    
    return <>
        <NavButton onClick={() => {
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