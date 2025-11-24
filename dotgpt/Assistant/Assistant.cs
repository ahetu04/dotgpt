using System.Text.Json;

namespace dotgpt
{
    //-----------------------------------------------
    // Assistant
    //-----------------------------------------------
    public partial class Assistant
    {
        public string Name { get; set; } = "default";

        public string Model { get; set; } = "gpt-5.1";

        public string Instructions { get; set; } = "You are a helpful AI assistant. Answer as concisely as possible.";

        public int PromptHistory { get; set; } = 10;

        private static string Filename(string profileName)
        {
            return $"{dotgpt.Utils.GetApplicationDataPath()}Assistants/{profileName}.json";
        }

        //-----------------------------------------------
        // Assistant::Create
        //-----------------------------------------------
        public static Assistant? Create(string name)
        {
            try
            {
                string filename = Assistant.Filename(name);

                if (File.Exists(filename))
                {
                    string fileContent = File.ReadAllText(filename);
                    Assistant? a = JsonSerializer.Deserialize<dotgpt.Assistant>(fileContent);

                    if (a != null)
                    {
                        return a;
                    }
                }
            }
            catch (Exception)
            {
            }

            Assistant newAssistant = new Assistant(name);
            newAssistant.Save();

            return newAssistant;
        }

        //-----------------------------------------------
        // Assistant::Assistant
        //-----------------------------------------------
        public Assistant()
        {
        }

        //-----------------------------------------------
        // Assistant::Assistant
        //-----------------------------------------------
        protected Assistant(string name)
        {
            this.Name = name;
        }

        //-----------------------------------------------
        // Assistant::Save
        //-----------------------------------------------
        public void Save()
        {
            try
            {
                string s = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string filename = Assistant.Filename(this.Name);
                if (filename != null)
                {
                    string? directoryPath = Path.GetDirectoryName(filename);
                    if (directoryPath != null && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    File.WriteAllText(filename, s);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}