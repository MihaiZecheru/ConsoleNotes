using Spectre.Console;

namespace ConsoleNotes;

public class UserSettings
{
    /// <summary>
    /// Prevents the SaveSettings() method from being called while the object is being constructed
    /// </summary>
    private bool constructing = false;

    /// <summary>
    /// Make the notes in Mode.ViewNotes be displayed in a rainbow pattern
    /// </summary>
    private bool showRainbowNotes;

    /// <summary>
    /// If true, the most recently written notes will be displayed first in Mode.ViewNotes
    /// </summary>
    private bool notesDisplayOrder_NewestFirst;

    /// <summary>
    /// If true, the date will be displayed as DD/MM/YYYY. If false, it will be displayed as MM/DD/YYYY
    /// </summary>
    private bool dateDayFirst;

    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+1 while writing/editing a note
    /// </summary>
    private string color1;

    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+2 while writing/editing a note
    /// </summary>
    private string color2;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+3 while writing/editing a note
    /// </summary>
    private string color3;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+4 while writing/editing a note
    /// </summary>
    private string color4;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+5 while writing/editing a note
    /// </summary>
    private string color5;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+6 while writing/editing a note
    /// </summary>
    private string color6;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+7 while writing/editing a note
    /// </summary>
    private string color7;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+8 while writing/editing a note
    /// </summary>
    private string color8;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+9 while writing/editing a note
    /// </summary>
    private string color9;
    
    /// <summary>
    /// Custom color to be inserted as markup when the user presses Ctrl+0 while writing/editing a note
    /// </summary>
    private string color0;

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
    public bool DateDayFirst
    {
        get => dateDayFirst;
        set
        {
            dateDayFirst = value;
            SaveSettings();
        }
    }
    public string Color1
    {
        get => color1;
        set {
            color1 = value;
            SaveSettings();
        }
    }
    public string Color2
    {
        get => color2;
        set {
            color2 = value;
            SaveSettings();
        }
    }
    public string Color3
    {
        get => color3;
        set {
            color3 = value;
            SaveSettings();
        }
    }
    public string Color4
    {
        get => color4;
        set {
            color4 = value;
            SaveSettings();
        }
    }
    public string Color5
    {
        get => color5;
        set {
            color5 = value;
            SaveSettings();
        }
    }
    public string Color6
    {
        get => color6;
        set {
            color6 = value;
            SaveSettings();
        }
    }
    public string Color7
    {
        get => color7;
        set {
            color7 = value;
            SaveSettings();
        }
    }
    public string Color8
    {
        get => color8;
        set {
            color8 = value;
            SaveSettings();
        }
    }
    public string Color9
    {
        get => color9;
        set {
            color9 = value;
            SaveSettings();
        }
    }
    public string Color0
    {
        get => color0;
        set {
            color0 = value;
            SaveSettings();
        }
    }

    /// <summary>
    /// The path to the settings.txt file
    /// </summary>
    public static string SettingsFilePath { get; } = @"C:\ConsoleNotes\settings.txt";

    /// <summary>
    /// Parse the settings.txt file and set the object's properties to the values in the file
    /// </summary>
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
            DateDayFirst = Convert.ToBoolean(lines[2].Split('=')[1]);

            // Each color looks like this in the file: Color{i}=hex

            Color1 = lines[3].Split('=')[1];
            Color2 = lines[4].Split('=')[1];
            Color3 = lines[5].Split('=')[1];
            Color4 = lines[6].Split('=')[1];
            Color5 = lines[7].Split('=')[1];
            Color6 = lines[8].Split('=')[1];
            Color7 = lines[9].Split('=')[1];
            Color8 = lines[10].Split('=')[1];
            Color9 = lines[11].Split('=')[1];
            Color0 = lines[12].Split('=')[1];
        }
        else
        {
            CreateFileWithDefaultSettings();
        }

        constructing = false;
    }

    /// <summary>
    /// Format the object for saving to the settings.txt file
    /// </summary>
    /// <returns>A string to write to the settings.txt file</returns>
    public override string ToString()
    {
        Console.WriteLine("asdjkhasjdhasjdasjdhjasdjkasdh");
        return $"ShowRainbowNotes={ShowRainbowNotes}\n" +
               $"NotesDisplayOrder_OldestFirst={NotesDisplayOrder_NewestFirst}\n" +
               $"DateDayFirst={DateDayFirst}\n" +
               $"Color1={Color1}\n" +
               $"Color2={Color2}\n" +
               $"Color3={Color3}\n" +
               $"Color4={Color4}\n" +
               $"Color5={Color5}\n" +
               $"Color6={Color6}\n" +
               $"Color7={Color7}\n" +
               $"Color8={Color8}\n" +
               $"Color9={Color9}\n" +
               $"Color0={Color0}";
    }

    /// <summary>
    /// Save the settings to the settings.txt file
    /// </summary>
    public void SaveSettings()
    {
        if (!constructing)
            File.WriteAllText(SettingsFilePath, this.ToString());
    }

    /// <summary>
    /// Create a settings.txt file with default settings
    /// </summary>
    private void CreateFileWithDefaultSettings()
    {
        constructing = true;

        // Set all settings to default values
        ShowRainbowNotes = DefaultSettings.ShowRainbowNotes;
        NotesDisplayOrder_NewestFirst = DefaultSettings.NotesDisplayOrder_NewestFirst;
        DateDayFirst = DefaultSettings.DateDayFirst;
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
