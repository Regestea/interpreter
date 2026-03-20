import './App.css'
import {Outlet} from "react-router";
import Nav from "./features/Nav/Nav.tsx";

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
