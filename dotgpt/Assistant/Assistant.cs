using System.Text.Json;

namespace dotgpt
{
    //-----------------------------------------------
    // Assistant
    //-----------------------------------------------
    public partial class Assistant
    {
        public string Name { get; set; } = "default";

        public string Model { get; set; } = "gpt-4o";

        public string Instructions { get; set; } = "You are a helpful AI assistant. Answer as concisely as possible.";

        public double Temperature { get; set; } = 0.5;

        public int MaxTokens { get; set; } = 4096;

        public int PromptHistory { get; set; } = 5;

        public string APIKey { protected get; set; } = "";

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
        // Assistant::UpdateSettings
        //-----------------------------------------------
        public void UpdateSettings(Settings settings)
        {
            bool bDirty = false;

            // set api key on profile
            if (!string.IsNullOrEmpty(settings.ApiKey))
            {
                this.APIKey = settings.ApiKey;
                bDirty = true;
            }

            // model
            if (!string.IsNullOrEmpty(settings.Model))
            {
                this.Model = settings.Model;
                bDirty = true;
            }

            // instructions
            if (!string.IsNullOrEmpty(settings.Instructions))
            {
                this.Instructions = settings.Instructions;
                bDirty = true;
            }

            // temperature
            if (settings.Temperature >= 0.0 && settings.Temperature <= 1.0)
            {
                this.Temperature = settings.Temperature;
                bDirty = true;
            }

            // tokens
            if (settings.MaxTokens >= 1 && settings.MaxTokens < 32536)
            {
                this.MaxTokens = settings.MaxTokens;
                bDirty = true;
            }

            // max messages
            if (settings.PromptHistory >= 0)
            {
                this.PromptHistory = settings.PromptHistory;
                bDirty = true;
            }

            if (bDirty)
            {
                this.Save();
            }
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