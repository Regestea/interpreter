import {Link} from "react-router";
import {faGithub, faReact} from '@fortawesome/free-brands-svg-icons'
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";


function Home() {
    return <>
        <p>homepage</p>
        <Link to="Settings"> to settings</Link>
        <FontAwesomeIcon icon={faGithub}/>
        <FontAwesomeIcon icon={faReact} style={{color: '#61dafb', animation: 'spin 2s linear infinite'}}/>
    </>;
}

export default Home;