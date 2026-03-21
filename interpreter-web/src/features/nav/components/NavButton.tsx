import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import type { IconDefinition } from "@fortawesome/fontawesome-svg-core";

type NavButtonProps = {
    icon: IconDefinition | string;
    onClick?: () => void;
    isActive?: boolean;
};

function NavButton({ icon, onClick, isActive }: NavButtonProps) {
    const isText = typeof icon === "string";

    return (
        <button
            onClick={onClick}
            className={`btn d-flex justify-content-center ${isActive ? "btn-primary" : ""}`}
            type="button"
        >
            {isText ? (
                <span className="align-self-center">{icon}</span>
            ) : (
                <FontAwesomeIcon
                    className="align-self-center"
                    icon={icon}
                />
            )}
        </button>
    );
}

export default NavButton;
