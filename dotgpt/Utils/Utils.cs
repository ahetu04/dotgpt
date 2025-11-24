namespace dotgpt
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
                ApplicationDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "/gpta/";
                ApplicationDataPath = ApplicationDataPath.Replace("\\", "/");
            }

            return ApplicationDataPath;
        }

        public static string RemoveSurroundingQuotes(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            if (input.Length >= 2 &&
                input[0] == '"' &&
                input[^1] == '"')
            {
                return input.Substring(1, input.Length - 2);
            }

            return input;
        }
    }
}