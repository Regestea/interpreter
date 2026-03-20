import NavButton from "./NavButton.tsx";
import {faRotateBack, faNoteSticky, faPlay} from "@fortawesome/free-solid-svg-icons";
import {useNavStore} from "../store/currentNavStore.ts";

type NavStudyProps = {};

function NavStudy() {

    const setCurrentNav = useNavStore((state) => state.setCurrentNav);
    
    return <>
        <NavButton onClick={()=>{setCurrentNav("home")}} icon={faRotateBack}/>
        <NavButton icon={faNoteSticky}/>
        <NavButton icon={faPlay}/>
    </>;
}

export default NavStudy;