namespace dotgpt
{
    public partial class Assistant
    {
        //-----------------------------------------------
        // Settings
        //-----------------------------------------------
        public class Settings
        {
            public bool Help = false;
            public bool Lists = false;

            public string Assistant = "";
            public string ApiKey = "";
            public string Instructions = "";
            public string Model = "";
            public double Temperature = -1.0;
            public int MaxTokens = -1;
            public int PromptHistory = -1;

            public string Session = "";

            public bool Reset = false;

            public string ErrorMsg = "";

            //-----------------------------------------------
            // Settings::Settings
            //-----------------------------------------------
            public Settings(string[] cmdLineArguments)
            {
                const string argShowHelp = "-help";
                const string argLists = "-lists";
                const string argAssistant = "-assistant:";
                const string argApiKey = "-key:";
                const string argInstructions = "-instructions:";
                const string argModel = "-model:";
                const string argTemperature = "-temp:";
                const string argMaxToken = "-tokens:";
                const string argHistory = "-history:";
                const string argLoadSession = "-session:";
                const string argReset = "-reset";

                if (cmdLineArguments == null || cmdLineArguments.Length == 0)
                {
                    return;
                }

                for (int i = 0; i < cmdLineArguments.Length; i++)
                {
                    string cmdLineArg = cmdLineArguments[i];

                    if (cmdLineArg.StartsWith(argShowHelp))
                    {
                        this.Help = true;
                        return;
                    }
                    else if (cmdLineArg.StartsWith(argLists))
                    {
                        this.Lists = true;
                    }
                    else if (cmdLineArg.StartsWith(argAssistant))
                    {
                        this.Assistant = cmdLineArg.Remove(0, argAssistant.Length);
                    }
                    else if (cmdLineArg.StartsWith(argApiKey))
                    {
                        this.ApiKey = cmdLineArg.Remove(0, argApiKey.Length);
                    }
                    else if (cmdLineArg.StartsWith(argInstructions))
                    {
                        this.Instructions = cmdLineArg.Remove(0, argInstructions.Length);
                    }
                    else if (cmdLineArg.StartsWith(argModel))
                    {
                        this.Model = cmdLineArg.Remove(0, argModel.Length);
                    }
                    else if (cmdLineArg.StartsWith(argTemperature))
                    {
                        string tmp = cmdLineArg.Remove(0, argTemperature.Length);
                        if (!double.TryParse(tmp, out this.Temperature))
                        {
                            this.ErrorMsg = "Temperature argument is invalid";
                            return;
                        }
                    }
                    else if (cmdLineArg.StartsWith(argMaxToken))
                    {
                        string tmp = cmdLineArg.Remove(0, argMaxToken.Length);
                        if (!int.TryParse(tmp, out this.MaxTokens))
                        {
                            this.ErrorMsg = "Tokens argument is invalid";
                            return;
                        }
                    }
                    else if (cmdLineArg.StartsWith(argHistory))
                    {
                        string tmp = cmdLineArg.Remove(0, argHistory.Length);
                        if (!int.TryParse(tmp, out this.PromptHistory))
                        {
                            this.ErrorMsg = "Max messages argument is invalid";
                            return;
                        }
                    }
                    else if (cmdLineArg.StartsWith(argLoadSession))
                    {
                        this.Session = cmdLineArg.Remove(0, argLoadSession.Length);
                    }
                    else if (cmdLineArg.StartsWith(argReset))
                    {
                        this.Reset = true;
                    }
                }
            }
        }
    }

}