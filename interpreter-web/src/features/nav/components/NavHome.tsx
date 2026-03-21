import { faBookOpen, faEarthAsia, faRobot } from "@fortawesome/free-solid-svg-icons";
import NavButton from "./NavButton.tsx";
import {useNavigate} from "react-router";

function NavHome() {

    const navigate = useNavigate();

    return (
        <>
            <NavButton onClick={() => {
                navigate("/study");
            }} icon={faBookOpen} />
            
            <NavButton onClick={() => {
                navigate("/translator");
            }} icon={faEarthAsia} />
            
            <NavButton onClick={() => {
                navigate("/assistance");
            }} icon={faRobot} />
        </>
    );
}

export default NavHome;
