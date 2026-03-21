import NavHome from "./components/NavHome.tsx";
import NavAssistance from "./components/NavAssistance.tsx";
import NavLiveTranslation from "./components/NavLiveTranslation.tsx";
import NavStudy from "./components/NavStudy.tsx";
import {useLocation} from "react-router";
import type {CurrentNav} from "./types/nav.types.ts";


function Nav() {
    
    const location = useLocation();

    const currentNav:CurrentNav = location.pathname.split("/").filter(Boolean)[0] as CurrentNav;
    

    const renderNavContent = () => {
        switch (currentNav) {
            case 'home':
                return <NavHome />;
            case 'study':
                return <NavStudy />;
            case 'translator':
                return <NavLiveTranslation />;
            case 'assistance':
                return <NavAssistance />;
            default:
                return <NavHome />;
        }
    };

    return (
        <nav className="navbar navbar-dark glass-nav fixed-bottom">
            <div className="container-fluid px-4">
                <div className="d-flex justify-content-center w-100">
                    {renderNavContent()}
                </div>
            </div>
        </nav>
    );
}

export default Nav;
