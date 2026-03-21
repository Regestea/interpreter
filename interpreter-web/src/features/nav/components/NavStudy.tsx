import NavButton from "./NavButton.tsx";
import {faRotateBack, faNoteSticky, faPlay} from "@fortawesome/free-solid-svg-icons";
import {useNavigate} from "react-router";


function NavStudy() {

    const navigate = useNavigate();
    
    return <>
        <NavButton onClick={() => {
            navigate("/");
        }} icon={faRotateBack}/>
        <NavButton icon={faNoteSticky}/>
        <NavButton icon={faPlay}/>
    </>;
}

export default NavStudy;