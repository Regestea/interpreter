import './App.css'
import {Outlet} from "react-router";

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
                      <button className="btn">Home</button>
                      <button className="btn">Dashboard</button>
                      <button className="btn">Settings</button>
                      <button className="btn">Profile</button>
                  </div>
              </div>
          </nav>
      </>
  )
}

export default App
