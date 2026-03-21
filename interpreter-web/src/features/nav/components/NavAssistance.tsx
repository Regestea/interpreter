import {faRotateBack} from "@fortawesome/free-solid-svg-icons";
import NavButton from "./NavButton.tsx";
import {useNavigate} from "react-router";

function NavAssistance() {
    const navigate = useNavigate();
    
    return <>
        <NavButton onClick={() => {
            navigate("/");
        }} icon={faRotateBack}/>
    </>;
}

export default NavAssistance;