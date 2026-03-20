import './App.css'
import {Outlet} from "react-router";
import {} from "@fortawesome/fontawesome-svg-core";
import {faBookOpen,
    faEarthAsia,
    faRobot} from "@fortawesome/free-solid-svg-icons";
import {FontAwesomeIcon} from "@fortawesome/react-fontawesome";

function App() {


  return (
      <>
          <div className="main-container">
              <div className="container-box">
                  <div className="w-100 h-100">
                      <Outlet />
                  </div>
              </div>
          </div>

          <nav className="navbar navbar-dark glass-nav fixed-bottom">
              <div className="container-fluid px-4">
                  <div className="d-flex justify-content-center w-100">
                      <button className="btn d-flex justify-content-center"><FontAwesomeIcon className="align-self-center" icon={faBookOpen}/></button>
                      <button className="btn d-flex justify-content-center"><FontAwesomeIcon className="align-self-center" icon={faEarthAsia}/></button>
                      <button className="btn d-flex justify-content-center"><FontAwesomeIcon className="align-self-center" icon={faRobot}/></button>
                  </div>
              </div>
          </nav>
      </>
  )
}

export default App
