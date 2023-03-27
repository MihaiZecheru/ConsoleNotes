﻿using Spectre.Console;

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
    private string color1;
    private string color2;
    private string color3;
    private string color4;
    private string color5;
    private string color6;
    private string color7;
    private string color8;
    private string color9;
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
    public bool CreateBackups
    {
        get => createBackups;
        set {
            createBackups = value;
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

        constructing = true;
    }

    public override string ToString()
    {
        return $"ShowRainbowNotes={ShowRainbowNotes}\n" +
               $"NotesDisplayOrder_OldestFirst={NotesDisplayOrder_NewestFirst}\n" +
               $"CreateBackups={CreateBackups}\n" +
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
