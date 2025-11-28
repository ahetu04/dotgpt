using dotgpt.OpenAI.Chat;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace dotgpt.gpta
{
    internal class CommandLine
    {
        protected static dotgpt.GlobalSettings? GlobalSettings = null;
        protected static Assistant? Assistant = null;
        protected static dotgpt.OpenAI.Chat.Session? Session = null;

        //-----------------------------------------------
        // Program::Main
        //-----------------------------------------------
        private static async Task<int> Main(string[] args)
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
            }

            if (args.Length > 0)
            {
                if (args[0] == "-help")
                {
                    PrintHelp();
                }
            }

            // load/create the global settings
            GlobalSettings = dotgpt.GlobalSettings.Load();
            if (GlobalSettings == null)
            {
                return -1;
            }

            // if API key is invalid, request it from the user
            if (string.IsNullOrEmpty(GlobalSettings.apiKey))
            {
                Console.WriteLine("Enter your API key (you only need to do this once): ");
                string? tmp = Console.ReadLine();

                if (!string.IsNullOrEmpty(tmp))
                {
                    GlobalSettings.apiKey = tmp;
                }

                if (string.IsNullOrEmpty(GlobalSettings.apiKey))
                {
                    Console.WriteLine("Invalid key!");
                    return -1;
                }

                GlobalSettings.Save();
            }

            // load/create the assistant
            Assistant = dotgpt.Assistant.Create(GlobalSettings.AssistantName);
            {
                if (Assistant == null)
                {
                    return -1;
                }
            }

            // load/create the session
            {
                SwitchSession(GlobalSettings.SessionName);
            }

            if (Session == null)
            {
                return -1;
            }

            List<string> promptsToQuit = new List<string>()
            {
                "quit", 
                "exit",
                "q",
                "/quit", 
                "/exit",
                "/q"
            };

            // main loop, back and forth between user and API
            ConsoleColor userColor = Console.ForegroundColor;
            while (true)
            {
                Console.ForegroundColor = userColor;
                Console.Write("You > ");
                string? prompt = Console.ReadLine();

                if (prompt == null)
                {
                    break;
                }

                // quit?
                if (promptsToQuit.Contains(prompt))
                {
                    break;
                }

                // command 
                if (prompt.StartsWith("/"))
                {
                    try
                    {
                        ProcessCommand(prompt);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                    continue;
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
                    dotgpt.OpenAI.Chat.Message m = await Session.EnterPrompt(prompt, onRoleChanged, onToken, onError);

                    Session.Save();
                }

                Console.WriteLine("\n");
            }

            return -1;
        }


        //-----------------------------------------------
        // Program::ProcessCommand
        //-----------------------------------------------
        public static void ProcessCommand(string command)
        {

            if (Assistant == null || Session == null || GlobalSettings == null)
            {
                return;
            }

            if (command == "/status")
            {
                ListAllAssistantsAndSessions(Assistant, Session);
                return;
            }

            if (command == "/clear")
            {
                Session.History.Clear();
                Session.Save();
                return;
            }

            if (command == "/reset")
            {
                // create a branch new 'default' assistant and save it
                Assistant = new Assistant();
                Assistant.Save();

                // then switch to that assistant
                SwitchAssistant("default");

                // and switch to the 'default' session
                SwitchSession("default");

                Session.History.Clear();
                Session.Save();

                return;
            }

            if (command == "/help")
            {
                PrintHelp();
                return;
            }

            string[] splitCommand = command.Split(" ", 2);
            if (splitCommand.Length != 2)
            {
                Console.WriteLine("Invalid command");
                return;
            }

            switch (splitCommand[0])
            {
                case "/key":
                {
                    GlobalSettings.apiKey = splitCommand[1];
                    GlobalSettings.Save();

                    Session.APIKey = splitCommand[1];
                    Session.Save();

                    break;
                }

                case "/m":
                case "/model":
                {
                    string modelName = splitCommand[1];
                    Assistant.Model = modelName;
                    Assistant.Save();

                    Session.Model = modelName;
                    Session.Save();

                    break;
                }

                case "/a":
                case "/assistant":
                {
                    string assistantName = splitCommand[1];
                    SwitchAssistant(assistantName);
                    break;
                }

                case "/h":
                case "/history":
                {
                    int historySize = 0;
                    if (int.TryParse(splitCommand[1], out historySize))
                    {
                        Assistant.PromptHistory = historySize;
                        Assistant.Save();

                        Session.PromptHistory = historySize;
                        Session.Save();
                    }
                    break;
                }

                case "/i":
                case "/instruction":
                case "/instructions":
                {
                    string instructions = splitCommand[1];
                    instructions = Utils.RemoveSurroundingQuotes(instructions);

                    Assistant.Instructions = instructions;
                    Assistant.Save();

                    Session.Instructions = instructions;
                    Session.Save();

                    break;
                }

                case "/s":
                case "/session":
                {
                    SwitchSession(splitCommand[1]);

                    GlobalSettings.SessionName = splitCommand[1];
                    GlobalSettings.Save();
                    break;
                }

                case "/savemd":
                {
                    SaveMarkdown(splitCommand[1]);
                    break;
                }

                default:
                {
                    Console.WriteLine($"Unknown command '{splitCommand[0]}'");
                    break;
                }
            }

        }

        //-----------------------------------------------
        // Program::SaveMarkdown
        //-----------------------------------------------
        protected static void SaveMarkdown(string InSavename)
        {
            if (Session == null)
            {
                return;
            }

            StringBuilder sb = new StringBuilder();

            foreach (Message m in Session.History)
            {
                sb.AppendLine($"## `{m.role}`\n");
                sb.AppendLine($"{m.content}\n\n");
            }

            string savePath = $"{dotgpt.Utils.GetApplicationDataPath()}/Saved";
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            File.WriteAllText($"{savePath}/{InSavename}.md", sb.ToString());
        }


        //-----------------------------------------------
        // Program::SwitchSession
        //-----------------------------------------------
        protected static void SwitchSession(string InSessionName)
        {
            // session expects GlobalSettings and an Assistant to be loaded
            if (GlobalSettings == null || Assistant == null)
            {
                return;
            }

            // validate session name


            dotgpt.OpenAI.Chat.Session? newSession = null;
            {
                newSession = dotgpt.OpenAI.Chat.Session.Load(InSessionName);

                if (newSession == null)
                {
                    // create new session
                    newSession = new dotgpt.OpenAI.Chat.Session(GlobalSettings.apiKey);
                }

                // pass parameters from profile to session
                newSession.Name = InSessionName;
                newSession.APIKey = GlobalSettings.apiKey;
                newSession.Model = Assistant.Model;
                newSession.Instructions = Assistant.Instructions;
                newSession.PromptHistory = Assistant.PromptHistory;

                Session = newSession;
                Session.Save();
            }

            if (GlobalSettings != null)
            {
                // update global settings
                GlobalSettings.SessionName = InSessionName;
                GlobalSettings.Save();
            }
        }

        //-----------------------------------------------
        // Program::SwitchAssistant
        //-----------------------------------------------
        protected static void SwitchAssistant(string InAssistantName)
        {

            Assistant? newAssistant = dotgpt.Assistant.Create(InAssistantName);

            if (newAssistant == null)
            {
                return;
            }

            Assistant = newAssistant;

            // update the session to use the assistant
            if (Session != null)
            {

                // update the session
                Session.Model = Assistant.Model;
                Session.Instructions = Assistant.Instructions;
                Session.PromptHistory = Assistant.PromptHistory;
                Session.Save();

            }

            // update global settings 
            if (GlobalSettings != null)
            {
                // update global settings
                GlobalSettings.AssistantName = InAssistantName;
                GlobalSettings.Save();
            }
        }

        //-----------------------------------------------
        // Program::PrintHelp
        //-----------------------------------------------
        public static void PrintHelp()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  gpta                          # after publishing");
            Console.WriteLine("  dotnet run --project cl-gpta  # while developing\n");

            Console.WriteLine("Slash commands (enter at the prompt):");
            Console.WriteLine("  /key <api-key>          Updates the stored OpenAI API key.");
            Console.WriteLine("  /assistant <name>       Switches or creates an assistant profile (model/instructions/history).");
            Console.WriteLine("  /session <name>         Loads or creates a chat session so you can keep parallel threads.");
            Console.WriteLine("  /instructions <text>    Sets the system prompt for the active assistant/session.");
            Console.WriteLine("  /model <model-name>     Changes the OpenAI chat model (default: gpt-5.1).");
            Console.WriteLine("  /history <n>            Decides how many previous turns are resent with each prompt.");
            Console.WriteLine("  /clear                  Wipes the current session history.");
            Console.WriteLine("  /reset                  Returns to the default assistant/session and clears their history.");
            Console.WriteLine("  /status                 Lists all assistants/sessions under %LOCALAPPDATA%/gpta.");
            Console.WriteLine("  /savemd <filename>      Exports the current session to Saved/<filename>.md.");
            Console.WriteLine("  /help                   Prints this overview.");
            Console.WriteLine("  /q /quit /exit          Leaves the prompt.");

            Console.WriteLine("\nAssistants, sessions, and settings are saved under %LOCALAPPDATA%/gpta so you can continue where you left off next time.");
            Console.WriteLine("When you're done, just type 'quit', 'exit', or 'q'.\n");
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
            Console.WriteLine($"\tName: {currentAssistant.Name}\n\tModel: {currentAssistant.Model}\n\tInstructions: {currentAssistant.Instructions}\n\tPromptHistory: {currentAssistant.PromptHistory}");
            Console.WriteLine($"\nCurrent session: \n\tName: {currentSession.Name}\n\tHistory size: {currentSession.History.Count}");

            Console.WriteLine();
        }
    }
}
