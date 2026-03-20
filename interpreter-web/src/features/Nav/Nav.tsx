import NavHome from "./components/NavHome.tsx";
import NavAssistance from "./components/NavAssistance.tsx";
import NavLiveTranslation from "./components/NavLiveTranslation.tsx";
import NavStudy from "./components/NavStudy.tsx";
import { useNavStore } from "./store/currentNavStore.ts";

function Nav() {

    const currentNav = useNavStore((state) => state.currentNav);

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
