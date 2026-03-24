import Nav from "../features/nav/Nav.tsx";
import {Outlet} from "react-router";
import './App.css'

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

         <Nav/>
      </>
  )
}

export default App
