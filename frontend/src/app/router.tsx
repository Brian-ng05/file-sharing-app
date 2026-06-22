import * as React from "react";


import {
    createBrowserRouter
} from "react-router-dom";


import MainLayout from "../components/layouts/MainLayout";


import UploadPage from "../pages/UploadPage";
import HistoryPage from "../pages/HistoryPage";
import ReviewPage from "../pages/ReviewPage";



const router = createBrowserRouter([

    {
        path: "/",

        element: <MainLayout />,


        children: [

            {
                index: true,

                element: <UploadPage />
            },


            {
                path: "history",

                element: <HistoryPage />
            },


            {
                path: "f/:code",

                element: <ReviewPage />
            }

        ]

    }

]);



export default router;