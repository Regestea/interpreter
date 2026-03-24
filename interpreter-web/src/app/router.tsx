// src/routes/router.tsx
import { createBrowserRouter } from "react-router";
import App from "./App.tsx";
import NotFound from "../shared/components/NotFound.tsx";
import Home from "../features/home/Home.tsx";
import Translator from "../features/translator/Translator.tsx";

export const router = createBrowserRouter([
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
                path: "translator",
                element: <Translator />,
            },
        ],
    },
]);
