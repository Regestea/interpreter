import {InitializeSettingsService} from "./shared/services/initializeSettings.service.ts";
import {QueryClient, QueryClientProvider} from "@tanstack/react-query";
import 'bootstrap/dist/css/bootstrap.min.css';
import { createRoot } from 'react-dom/client';
import { RouterProvider } from "react-router";
import { router } from './app/router.tsx';
import { StrictMode } from 'react';
import './index.css';

const queryClient = new QueryClient();
InitializeSettingsService.run();

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
        </QueryClientProvider>
    </StrictMode>
);
