import { faBookOpen, faEarthAsia, faRobot } from "@fortawesome/free-solid-svg-icons";
import NavButton from "./NavButton.tsx";
import { useNavStore } from "../store/currentNavStore.ts";
import {useNavigate} from "react-router";

function NavHome() {

    const navigate = useNavigate();
    const setCurrentNav = useNavStore((state) => state.setCurrentNav);

    return (
        <>
            <NavButton onClick={() => {
                setCurrentNav("study");
                navigate("/study");
            }} icon={faBookOpen} />
            
            <NavButton onClick={() => {
                setCurrentNav("translator");
                navigate("/translator");
            }} icon={faEarthAsia} />
            
            <NavButton onClick={() => {
                setCurrentNav("assistance");
                navigate("/assistance");
            }} icon={faRobot} />
        </>
    );
}

export default NavHome;
