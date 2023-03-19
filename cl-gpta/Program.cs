using dotgpt.OpenAI.Chat;
using System.Text;

namespace dotgpt.gpta
{
    internal class CommandLine
    {
        //-----------------------------------------------
        // Program::Main
        //-----------------------------------------------
        private static async Task<int> Main(string[] args)
        {

            Console.InputEncoding = Encoding.Unicode;
            Console.OutputEncoding = Encoding.UTF8;

            // load global settings
            dotgpt.GlobalSettings? globalSettings = dotgpt.GlobalSettings.Load();
            if (globalSettings == null)
            {
                return -1;
            }

            // load all arguments from command line
            dotgpt.Assistant.Settings arguments = new dotgpt.Assistant.Settings(args);
            if (!string.IsNullOrEmpty(arguments.ErrorMsg))
            {
                Console.WriteLine(arguments.ErrorMsg);
                return -1;
            }

            // show help
            if (arguments.Help)
            {
                PrintHelp();
                return 0;
            }

            // update global settings
            {
                if (!string.IsNullOrEmpty(arguments.Assistant) && globalSettings.ProfileName != arguments.Assistant)
                {
                    globalSettings.ProfileName = arguments.Assistant;
                    globalSettings.Save();
                }

                if (!string.IsNullOrEmpty(arguments.Session) && globalSettings.SessionName != arguments.Session)
                {
                    globalSettings.SessionName = arguments.Session;
                    globalSettings.Save();
                }

                if (!string.IsNullOrEmpty(arguments.ApiKey) && globalSettings.apiKey != arguments.ApiKey)
                {
                    globalSettings.apiKey = arguments.ApiKey;
                    globalSettings.Save();
                }
            }

            // if API key is invalid, request it from the user
            if (string.IsNullOrEmpty(globalSettings.apiKey))
            {
                Console.WriteLine("Please enter your API key: ");
                string? tmp = Console.ReadLine();

                if (!string.IsNullOrEmpty(tmp))
                {
                    globalSettings.apiKey = tmp;
                }

                if (string.IsNullOrEmpty(globalSettings.apiKey))
                {
                    Console.WriteLine("Invalid key!");
                    return -1;
                }

                globalSettings.Save();
            }

            if (arguments.Reset)
            {
                new Assistant().Save();
                new dotgpt.OpenAI.Chat.Session("").Save();

                globalSettings.ProfileName = "default";
                globalSettings.SessionName = "default";

                globalSettings.Save();
            }

            // load assistant
            dotgpt.Assistant? assistant = dotgpt.Assistant.Create(globalSettings.ProfileName);
            {
                if (assistant == null)
                {
                    return -1;
                }

                assistant.UpdateSettings(arguments);
            }

            // load session
            dotgpt.OpenAI.Chat.Session? session = null;
            {
                string sessionName = globalSettings.SessionName;
                session = dotgpt.OpenAI.Chat.Session.Load(sessionName);

                if (session == null)
                {
                    // create new session
                    session = new dotgpt.OpenAI.Chat.Session(globalSettings.apiKey);
                }

                // pass parameters from profile to session
                session.Name = sessionName;
                session.APIKey = globalSettings.apiKey;
                session.Model = assistant.Model;
                session.Instructions = assistant.Instructions;
                session.Temperature = assistant.Temperature;
                session.MaxTokens = assistant.MaxTokens;
                session.PromptHistory = assistant.PromptHistory;
            }

            if (arguments.Lists)
            {
                ListAllAssistantsAndSessions(assistant, session);
                return 0;
            }

            // main loop, back and forth between user and API
            ConsoleColor userColor = Console.ForegroundColor;
            while (true)
            {
                Console.ForegroundColor = userColor;
                Console.Write("You > ");
                string? prompt = Console.ReadLine();

                if (prompt == "quit" || prompt == "exit" || prompt == "q")
                {
                    break;
                }

                if (!string.IsNullOrEmpty(prompt))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    var onRoleChanged = (string role) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Blue;
                        Console.Write($"\n{role} > ");
                    };
                    var onToken = (string token) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(token);
                    };
                    var onError = (string error) =>
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.Write($"\nError! {error}");
                    };
                    dotgpt.OpenAI.Chat.Message m = await session.EnterPrompt(prompt, onRoleChanged, onToken, onError);

                    session.Save();
                }

                Console.WriteLine("\n");
            }

            return -1;
        }

        //-----------------------------------------------
        // Program::PrintHelp
        //-----------------------------------------------
        public static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("gpta -profile:[PROFILE-NAME] -key:[YOUR-API-KEY] -instructions:[INSTRUCTIONS] -model:[MODEL] -temp:[TEMPERATURE] -tokens:[MAX-TOKENS] -messages:[MAX-MESSAGES] -session:[SESSION-NAME] -reset -lists -help");

            Console.WriteLine("\nOptions:");
            Console.WriteLine("   key : Sets the API key linked to your OpenAI account.");
            Console.WriteLine("   assistant : Switches the current assistant. If the assistant doesn't exist, it is created. Default is 'default'.");
            Console.WriteLine("   instructions : These are the instructions that your assistant will follow for each prompt you send. Default is \"You are a helpful AI assistant. Answer as concisely as possible.\"");
            Console.WriteLine("   model : Allows you to try out new models in the future, assuming that the chat API will remain the same. Default is 'gpt-3.5-turbo'. ");
            Console.WriteLine("   temp : Tweaks the temperature. Default is 0.5.");
            Console.WriteLine("   tokens : Sets the maximum number of tokens allowed for the answer. Default is 1024.");
            Console.WriteLine("   history : Sets the number of previous messages that should be resent with each prompt. Default is 5.");
            Console.WriteLine("   session : Creates and/or switch to a new chat session. The default session name is 'default'.");
            Console.WriteLine("   reset : Sets the current assistant to 'default' and resets its settings. Also sets the session to default and clears its history. ");
            Console.WriteLine("   lists : Lists all assistants and sessions available. Also lists the current assistant's settings.");
            Console.WriteLine("   help : Prints information about the command line arguments. ");

            Console.WriteLine("\nExamples:");
            Console.WriteLine("   ./gpta -key:\"YOUR-OPENAI-KEY\" -assistant:\"git expert\" -instructions:\"You are an AI assistant good at solving problems with Git\" -model:gpt-3.5-turbo -temp:0.6 -tokens:1500 -history:4 -session:git -reset -lists -help");
            Console.WriteLine("   ./gpta -key:\"Your new key\"");
            Console.WriteLine("   ./gpta -assistant:FrenchTranslator");
            Console.WriteLine("   ./gpta -instructions:\"You are a French translator. Translate ...\"");
            Console.WriteLine("   ./gpta -reset");
            Console.WriteLine("   ./gpta");

            Console.WriteLine("\nNote: Your profile and session will persist between instances of gpta. Use the '-lists' option to display the current profile used and session, and the -profile and -session options to change them. ");

            Console.WriteLine("\n\n");
        }

        //-----------------------------------------------
        // Program::ListAllAssistantsAndSessions
        //-----------------------------------------------
        public static void ListAllAssistantsAndSessions(Assistant currentAssistant, Session currentSession)
        {
            Console.WriteLine();

            ConsoleColor userColor = Console.ForegroundColor;

            // list all assistants
            {
                Console.WriteLine("Assistants:");
                string[]? files = null;
                try
                {
                    files = Directory.GetFiles($"{dotgpt.Utils.GetApplicationDataPath()}Assistants/");
                }
                catch (Exception)
                {
                    files = null;
                }

                if (files != null)
                {
                    foreach (string f in files)
                    {
                        if (Path.GetFileNameWithoutExtension(f) == currentAssistant.Name)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  *{Path.GetFileNameWithoutExtension(f)}");
                        }
                        else
                        {
                            Console.ForegroundColor = userColor;
                            Console.WriteLine($"   {Path.GetFileNameWithoutExtension(f)}");
                        }
                    }
                }
                Console.ForegroundColor = userColor;
            }

            // list all sessions
            {
                Console.WriteLine("\nSessions:");
                string[]? files = null;
                try
                {
                    files = Directory.GetFiles($"{dotgpt.Utils.GetApplicationDataPath()}Sessions/");
                }
                catch (Exception)
                {
                    files = null;
                }

                if (files != null)
                {
                    foreach (string f in files)
                    {
                        if (Path.GetFileNameWithoutExtension(f) == currentSession.Name)
                        {
                            Console.ForegroundColor = ConsoleColor.Green;
                            Console.WriteLine($"  *{Path.GetFileNameWithoutExtension(f)}");
                        }
                        else
                        {
                            Console.ForegroundColor = userColor;
                            Console.WriteLine($"   {Path.GetFileNameWithoutExtension(f)}");
                        }
                    }
                }
                Console.ForegroundColor = userColor;
            }

            Console.WriteLine("");
            Console.WriteLine($"Current assistant: ");
            Console.WriteLine($"\tName: {currentAssistant.Name}\n\tModel: {currentAssistant.Model}\n\tInstructions: {currentAssistant.Instructions}\n\tTemperature: {currentAssistant.Temperature}\n\tMaxTokens: {currentAssistant.MaxTokens}\n\tPromptHistory: {currentAssistant.PromptHistory}");
            Console.WriteLine($"\nCurrent session: \n\tName: {currentSession.Name}\n\tHistory size: {currentSession.History.Count}");

            Console.WriteLine();
        }
    }
}