using dotgpt.OpenAI.Chat;
using System.Runtime.InteropServices;
using System.Text;

namespace dotgpt.mdt
{
    internal class CommandLine
    {
        private static async Task<int> Main(string[] args)
        {

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.InputEncoding = Encoding.Unicode;
                Console.OutputEncoding = Encoding.Unicode;
            }

            if (args.Length < 2)
            {
                Console.WriteLine("Markdown translate");
                Console.WriteLine("usage: mdt {path to markdown document} {language}");
                return -1;
            }

            string fileToTranslate = args[0];
            string languageToTranslateTo = args[1];

            fileToTranslate = Path.GetFullPath(fileToTranslate);
            if (fileToTranslate == null || !File.Exists(fileToTranslate))
            {
                Console.WriteLine("Failed to find file to translate");
                return -1;
            }

            string? directoryName = Path.GetDirectoryName(fileToTranslate);

            // clear target file
            string filetoWrite = directoryName + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(fileToTranslate)+$"-{languageToTranslateTo}" + Path.GetExtension(fileToTranslate);
            if (File.Exists(filetoWrite))
            {
                try
                {
                    File.Delete(filetoWrite);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to remove target file: " + e.ToString());
                    return -1;
                }

            }

            // break down input file into lines
            string[] lines = File.ReadAllText(fileToTranslate).Split(new char[] { '\n' }, StringSplitOptions.None);

            // load dotpgt global settings
            dotgpt.GlobalSettings? globalSettings = dotgpt.GlobalSettings.Load();
            if (globalSettings == null)
            {
                return -1;
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

            dotgpt.OpenAI.Chat.Session session = new dotgpt.OpenAI.Chat.Session(globalSettings.apiKey)
            {
                Name = "markdown translator",
                APIKey = globalSettings.apiKey,
                Instructions = $"You are a Markdown translation tool. You will be given text formatted for Markdown documents and will translate it directly to {languageToTranslateTo}. Do not add anything else beside the translated text, and preserve all Markdown formatting characters. Here is the text: ",
                Temperature = 0.5f,
                MaxTokens = 3500,
                PromptHistory = 0
            };

            Func<string, bool> isLineEmpty = (string line) =>
            {
                // quick test
                if (line == "\n" || line == "\r" || line == "\r\n" || line == "")
                {
                    return true;

                }

                string lineWithoutLineEndings = line.ReplaceLineEndings("");


                // go through every character until we find one that isn't a space or a tab
                foreach (char c in lineWithoutLineEndings)
                {
                    if (c != ' ' && c != '\t')
                    {
                        return false;
                    }
                }

                // yup, empty
                return true;

            };

            Func<string, string> getSpacesAndTabsAtBeginningOfLine = (string line) =>
            {
                string result = "";

                // go through every character until we find one that isn't a space or a tab
                foreach (char c in line)
                {
                    if (c == ' ' || c == '\t')
                    {
                        result += c;
                    }
                    else
                    {
                        break;
                    }
                }

                return result;

            };

            Func<string, string> getSpacesAndTabsAtEndOfLine = (string line) =>
            {
                int best = -1;
                int position = line.Length - 1;
                while (position >= 0 && line[position] == ' ' || line[position] == '\t')
                {
                    best = position;
                    position--;
                }

                if (best != -1)
                {
                    return line.Substring(best);
                }

                return "";

            };


            foreach (string currentLine in lines)
            {
                // if we hit an empty line, append empty line to the target file
                if (isLineEmpty(currentLine))
                {
                    File.AppendAllText(filetoWrite, "\n");
                    Console.WriteLine();
                    continue;
                }

                // remove line ending character from the prompt
                string prePrompt = getSpacesAndTabsAtBeginningOfLine(currentLine);
                string prompt = currentLine.ReplaceLineEndings("");
                string postPrompt = getSpacesAndTabsAtEndOfLine(currentLine);

                prompt = prompt.Trim();

                Console.ForegroundColor = ConsoleColor.Green;
                var onRoleChanged = (string role) => { };
                var onToken = (string token) =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(token);
                };
                var onError = (string error) =>
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write($"\nError! {error}\n");
                };

                // keep trying until we get an answer
                dotgpt.OpenAI.Chat.Message? m = null;
                while (m == null || m.role == "Exception" || m.role == "Failed")
                {
                    m = await session.EnterPrompt(prompt, onRoleChanged, onToken, onError);
                }

                // result might require additional cleanup, make sure it didn't insert line ending characters
                m.content = m.content.ReplaceLineEndings("");

                if (m != null && !string.IsNullOrEmpty(m.content))
                {
                    File.AppendAllText(filetoWrite, $"{prePrompt}{m.content}{postPrompt}");
                    File.AppendAllText(filetoWrite, "\n");
                }

                Console.WriteLine();

            }

            return 0;

        }

    }

}