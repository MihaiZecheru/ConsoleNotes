using Spectre.Console;

namespace ConsoleNotes;

public class UserSettings
{
    /// <summary>
    /// Prevents the SaveSettings() method from being called while the object is being constructed
    /// </summary>
    private bool constructing = false;

    private bool showRainbowNotes;
    private bool notesDisplayOrder_NewestFirst;
    private bool createBackups;
    private Color color1;
    private Color color2;
    private Color color3;
    private Color color4;
    private Color color5;
    private Color color6;
    private Color color7;
    private Color color8;
    private Color color9;
    private Color color0;

    public bool ShowRainbowNotes {
        get => showRainbowNotes;
        set {
            showRainbowNotes = value;
            SaveSettings();
        }
    }
    public bool NotesDisplayOrder_NewestFirst
    {
        get => notesDisplayOrder_NewestFirst;
        set {
            notesDisplayOrder_NewestFirst = value;
            SaveSettings();
        }
    }
    public bool CreateBackups
    {
        get => createBackups;
        set {
            createBackups = value;
            SaveSettings();
        }
    }
    public Color Color1
    {
        get => color1;
        set {
            color1 = value;
            SaveSettings();
        }
    }
    public Color Color2
    {
        get => color2;
        set {
            color2 = value;
            SaveSettings();
        }
    }
    public Color Color3
    {
        get => color3;
        set {
            color3 = value;
            SaveSettings();
        }
    }
    public Color Color4
    {
        get => color4;
        set {
            color4 = value;
            SaveSettings();
        }
    }
    public Color Color5
    {
        get => color5;
        set {
            color5 = value;
            SaveSettings();
        }
    }
    public Color Color6
    {
        get => color6;
        set {
            color6 = value;
            SaveSettings();
        }
    }
    public Color Color7
    {
        get => color7;
        set {
            color7 = value;
            SaveSettings();
        }
    }
    public Color Color8
    {
        get => color8;
        set {
            color8 = value;
            SaveSettings();
        }
    }
    public Color Color9
    {
        get => color9;
        set {
            color9 = value;
            SaveSettings();
        }
    }
    public Color Color0
    {
        get => color0;
        set {
            color0 = value;
            SaveSettings();
        }
    }

    public static string SettingsFilePath { get; } = @"C:\ConsoleNotes\settings.txt";

    public UserSettings()
    {
        constructing = true;
        if (File.Exists(SettingsFilePath))
        {
            using StreamReader streamReader = File.OpenText(SettingsFilePath);
            string text = streamReader.ReadToEnd();
            string[] lines = text.Split('\n');

            // File follows the same order as the properties up top
            ShowRainbowNotes = Convert.ToBoolean(lines[0].Split('=')[1]);
            NotesDisplayOrder_NewestFirst = Convert.ToBoolean(lines[1].Split('=')[1]);
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

        constructing = true;
    }

    public override string ToString()
    {
        return $"ShowRainbowNotes={ShowRainbowNotes}\n" +
               $"NotesDisplayOrder_OldestFirst={NotesDisplayOrder_NewestFirst}\n" +
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
        if (!constructing)
            File.WriteAllText(SettingsFilePath, this.ToString());
    }

    private void CreateFileWithDefaultSettings()
    {
        constructing = true;

        // Set all settings to default values
        ShowRainbowNotes = DefaultSettings.ShowRainbowNotes;
        NotesDisplayOrder_NewestFirst = DefaultSettings.NotesDisplayOrder_NewestFirst;
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

        constructing = false;

        // Write settings to file
        SaveSettings();
    }
}
