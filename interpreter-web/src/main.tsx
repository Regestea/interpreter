import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import '../src/assets/bootstrap.min.css';
import './index.css'

import App from './App.tsx'
import {createBrowserRouter, RouterProvider} from "react-router";
import NotFound from "./common/components/NotFound.tsx";
import Settings from "./features/Settings/Settings.tsx";
import Home from "./features/Home.tsx";

const router = createBrowserRouter([
    {
        path: "/",
        element: <App />,
        errorElement: <NotFound />, // Handles 404 and other errors
        children: [
            {
                index: true, // This acts as the default route ("/")
                element: <Home />,
            },
            {
                path: "Settings",
                element: <Settings />,
            },
        ],
    },
]);

createRoot(document.getElementById('root')!).render(
  <StrictMode>
      <RouterProvider router={router} />
  </StrictMode>,
)
