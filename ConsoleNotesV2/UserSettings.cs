using Spectre.Console;

namespace ConsoleNotes;

public class UserSettings
{
    public bool ShowRainbowNotes { get; set; }
    public bool NotesDisplayOrder_OldestFirst { get; set; }
    public bool CreateBackups { get; set; }
    public Color Color1 { get; set; }
    public Color Color2 { get; set; }
    public Color Color3 { get; set; }
    public Color Color4 { get; set; }
    public Color Color5 { get; set; }
    public Color Color6 { get; set; }
    public Color Color7 { get; set; }
    public Color Color8 { get; set; }
    public Color Color9 { get; set; }
    public Color Color0 { get; set; }

    public static string SettingsFilePath { get; } = @"C:\ConsoleNotes\settings.txt";

    public UserSettings()
    {
        if (File.Exists(SettingsFilePath))
        {
            using StreamReader streamReader = File.OpenText(SettingsFilePath);
            string text = streamReader.ReadToEnd();
            string[] lines = text.Split('\n');

            // File follows the same order as the properties up top
            ShowRainbowNotes = Convert.ToBoolean(lines[0].Split('=')[1]);
            NotesDisplayOrder_OldestFirst = Convert.ToBoolean(lines[1].Split('=')[1]);
            CreateBackups = Convert.ToBoolean(lines[2].Split('=')[1]);
            
            // Each color looks like this in the file: Color{i}=r-g-b
            // lines[i].Split('=') gets the r-g-b, then .Split('-') separates the r, g, & b

            string[] rgb;
            rgb = lines[3].Split('=')[1].Split('-');
            Color1 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[4].Split('=')[1].Split('-');
            Color2 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[5].Split('=')[1].Split('-');
            Color3 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[6].Split('=')[1].Split('-');
            Color4 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[7].Split('=')[1].Split('-');
            Color5 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[8].Split('=')[1].Split('-');
            Color6 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[9].Split('=')[1].Split('-');
            Color7 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[10].Split('=')[1].Split('-');
            Color8 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[11].Split('=')[1].Split('-');
            Color9 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
            
            rgb = lines[12].Split('=')[1].Split('-');
            Color0 = new Color(Convert.ToByte(rgb[0]), Convert.ToByte(rgb[1]), Convert.ToByte(rgb[2]));
        }
        else
        {
            CreateFileWithDefaultSettings();
        }
    }

    public override string ToString()
    {
        return $"ShowRainbowNotes={ShowRainbowNotes}\n" +
               $"NotesDisplayOrder_OldestFirst={NotesDisplayOrder_OldestFirst}\n" +
               $"CreateBackups={CreateBackups}\n" +
               $"Color1={Color1.R}-{Color1.G}-{Color1.B}\n" +
               $"Color2={Color2.R}-{Color2.G}-{Color2.B}\n" +
               $"Color3={Color3.R}-{Color3.G}-{Color3.B}\n" +
               $"Color4={Color4.R}-{Color4.G}-{Color4.B}\n" +
               $"Color5={Color5.R}-{Color5.G}-{Color5.B}\n" +
               $"Color6={Color6.R}-{Color6.G}-{Color6.B}\n" +
               $"Color7={Color7.R}-{Color7.G}-{Color7.B}\n" +
               $"Color8={Color8.R}-{Color8.G}-{Color8.B}\n" +
               $"Color9={Color9.R}-{Color9.G}-{Color9.B}\n" +
               $"Color0={Color0.R}-{Color0.G}-{Color0.B}";
    }

    public void SaveSettings()
    {
        File.WriteAllText(SettingsFilePath, this.ToString());
    }

    private void CreateFileWithDefaultSettings()
    {
        // Set all settings to default values
        ShowRainbowNotes = DefaultSettings.ShowRainbowNotes;
        NotesDisplayOrder_OldestFirst = DefaultSettings.NotesDisplayOrder_OldestFirst;
        CreateBackups = DefaultSettings.CreateBackups;
        Color1 = DefaultSettings.Color1;
        Color2 = DefaultSettings.Color2;
        Color3 = DefaultSettings.Color3;
        Color4 = DefaultSettings.Color4;
        Color5 = DefaultSettings.Color5;
        Color6 = DefaultSettings.Color6;
        Color7 = DefaultSettings.Color7;
        Color8 = DefaultSettings.Color8;
        Color9 = DefaultSettings.Color9;
        Color0 = DefaultSettings.Color0;

        // Write settings to file
        SaveSettings();
    }
}
