import {useNavStore} from "../store/currentNavStore.ts";
import {faRotateBack} from "@fortawesome/free-solid-svg-icons";
import NavButton from "./NavButton.tsx";

type NavAssistanceProps = {};

function NavAssistance() {
    const setCurrentNav = useNavStore((state) => state.setCurrentNav);

    return <>
        <NavButton onClick={()=>{setCurrentNav("home")}} icon={faRotateBack}/>
    </>;
}

export default NavAssistance;