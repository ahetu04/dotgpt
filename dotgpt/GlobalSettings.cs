using System.Text.Json;

namespace dotgpt
{
    //-----------------------------------------------
    // GlobalSettings
    //-----------------------------------------------
    public class GlobalSettings
    {
        public string ProfileName { get; set; } = "default";
        public string SessionName { get; set; } = "default";
        public string apiKey { get; set; } = "";

        //-----------------------------------------------
        // GlobalSettings::Load
        //-----------------------------------------------
        public static GlobalSettings? Load()
        {
            try
            {
                string filename = $"{dotgpt.Utils.GetApplicationDataPath()}settings.json";

                if (File.Exists(filename))
                {
                    string fileContent = File.ReadAllText(filename);
                    GlobalSettings? settings = JsonSerializer.Deserialize<GlobalSettings>(fileContent);

                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch (Exception)
            {
            }

            GlobalSettings gs = new GlobalSettings();
            gs.Save();

            return gs;
        }

        //-----------------------------------------------
        // GlobalSettings::Save
        //-----------------------------------------------
        public void Save()
        {
            try
            {
                string s = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                string filename = $"{dotgpt.Utils.GetApplicationDataPath()}settings.json";
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