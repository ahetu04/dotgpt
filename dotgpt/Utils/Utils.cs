﻿namespace dotgpt
{
    //-----------------------------------------------
    // Utils
    //-----------------------------------------------
    public class Utils
    {
        private static string ApplicationDataPath = "";

        //-----------------------------------------------
        // Utils::GetApplicationDataPath
        //-----------------------------------------------
        public static string GetApplicationDataPath
            (
            )
        {
            if (string.IsNullOrEmpty(ApplicationDataPath))
            {
                ApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/gpta/";
                ApplicationDataPath = ApplicationDataPath.Replace("\\", "/");
            }

            return ApplicationDataPath;
        }
    }
}