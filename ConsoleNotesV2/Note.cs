﻿using Spectre.Console;
using System.Text.RegularExpressions;
using Spectre.Console.Json;

namespace ConsoleNotes;

public class Note
{
    /// <summary>
    /// Regex for finding links in a string
    /// </summary>
    private static Regex LinksRegex = new Regex(@"(https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,})");

    /// <summary>
    /// Used to separate notes in the notes.txt file
    /// </summary>
    public static string NoteSeparator = "{<@SEP>}";

    /// <summary>
    /// Used to separate the date from the title of a note
    /// </summary>
    public static string DateSeparator = "{<@DATE>}";

    /// <summary>
    /// Use to separate the title of a note from the body, with the title appearing on the left side of the separator
    /// </summary>
    public static string TitleSeparator = "{<@TITLE>}";

    /// <summary>
    /// The string used to represent a note without a title
    /// </summary>
    public static string EmptyTitle = "{{TITLE_EMPTY}}";

    /// <summary>
    /// Indicates whether a note is JSON-only
    /// </summary>
    public static string IsJsonIndicator = "{<@JSON-ONLY>}";

    /// <summary>
    /// Used for checking if a note doesn't have a title
    /// </summary>
    private bool NoTitle;

    /// <summary>
    /// The title of the note
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The content in the note
    /// </summary>
    public string Body { get; set; }

    /// <summary>
    /// Datetime string of the time the note was created
    /// </summary>
    private readonly string createdAt;

    /// <summary>
    /// Will automatically format the <see cref="createdAt"/> datetime based on the user's <see cref="DefaultSettings.DateDayFirst"/> setting
    /// </summary>
    public string CreatedAt
    {
        get
        {
            DateTime datestamp = DateTime.ParseExact(createdAt, "dd/MM/yyyy HH:mm", null);
            return Program.Settings.DateDayFirst ? datestamp.ToString("dd/MM/yyyy HH:mm") : datestamp.ToString("MM/dd/yyyy HH:mm");
        }
    }

    /// <summary>
    /// If the note is JSON-only, meaning the body is a JSON object with no additional text
    /// </summary>
    public bool IsJson;

    /// <summary>
    /// For making a new note that doesn't already exist
    /// </summary>
    /// <param name="title">The title of the note</param>
    /// <param name="body">The content of the note</param>
    public Note(string title, string body, bool isJson)
    {
        Title = title;
        Body = body;
        NoTitle = (Title == EmptyTitle);
        createdAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        IsJson = isJson;
    }

    /// <summary>
    /// For turning the raw text of a note from the notes.txt file and creating a Note object from it
    /// </summary>
    /// <param name="raw_note_content">The raw text from the notes.txt file, which still includes a title separator</param>
    public Note(string raw_note_content)
    {
        // Check if note is JSON-only
        IsJson = raw_note_content.StartsWith(IsJsonIndicator);
        if (IsJson)
        {
            raw_note_content = raw_note_content.Substring(IsJsonIndicator.Length);
        }

        // Separate note into date, title, and body
        string[] note = raw_note_content.Split(TitleSeparator);
        string[] titleAndDate = note[0].Split(DateSeparator); // [0] = empty string, [1] = date, [2] = title
        string body = note[1]; // Just the body

        // Set attributes
        createdAt = titleAndDate[1];
        Title = titleAndDate[2];
        Body = body;
        NoTitle = (Title == EmptyTitle);
    }

    /// <summary>
    /// Turn the note into a <see cref="Panel"/>,
    /// which can be displayed to the screen with <see cref="AnsiConsole.Write()"/>
    /// </summary>
    /// <remarks>
    /// The panel's border will be given the next color from the ColorCycle
    /// <br/><br/>
    /// The color of the border is returned so that the <see cref="Rule"/>
    /// which displays the Date of the note can be given the same color
    /// </remarks>
    /// <returns>
    /// Item1: <see cref="Panel"/> to display on the console<br/>
    /// Item2: The <see cref="Color"/> used for the panel's border
    /// </returns>
    private Tuple<Panel, Color> GetAsPanel()
    {
        // Panel and panel header
        Panel panel;
        if (IsJson)
        {
            panel = new Panel(new JsonText(Body).CommaColor(Color.Silver).BracketColor(Color.Silver).BracesColor(Color.Silver));
        }
        else
        {
            panel = new Panel(new Markup(Body));
        }

        if (!NoTitle) panel.Header = new PanelHeader(Title);

        // Add ColorCycle border color only if user has rainbow notes enabled
        Color color;
        if (Program.Settings.ShowRainbowNotes)
        {
            color = ColorCycle.Next();
            panel.BorderStyle = new Style(color);
        }
        else
        {
            color = Color.Default;
        }

        // Expand the panel before returning
        return Tuple.Create(panel.Expand(), color);
    }

    /// <summary>
    /// Write the note to the console inside of a <see cref="Panel"/>,
    /// with the note's date as its header
    /// </summary>
    public void Display()
    {
        (Panel noteAsPanel, Color color) = GetAsPanel();

        // Header rule
        Rule dateDisplay = new Rule(CreatedAt);
        dateDisplay.Style = new Style(color);

        // Display the note
        AnsiConsole.Write(dateDisplay);
        AnsiConsole.Write(noteAsPanel);
    }

    /// <summary>
    /// Convert the note to a string for storing to notes.txt
    /// </summary>
    /// <remarks>
    /// Follows the format of the notes.txt file, and is used for saving notes to the file
    /// </remarks>
    /// <returns>The stringified note</returns>
    public override string ToString()
    {
        return (IsJson ? IsJsonIndicator : "")
            + $"{DateSeparator}{createdAt}{DateSeparator}{Title}{TitleSeparator}{Body}";
    }

    /// <summary>
    /// Replace every instance of a URL with a URL surrounded by link markup tags [link] and [/]
    /// </summary>
    /// <remarks>
    /// Used for creating clickable links in the console
    /// </remarks>
    /// <param name="text">The text to parse for links</param>
    /// <returns>The <paramref name="text"/> with all instances of a URL wrapped in a [link][/] tag</returns>
    public static string ParseLinksMarkup(string text)
    {
        foreach (Match match in LinksRegex.Matches(text))
        {
            string url = match.Value;
            text = text.Replace(url, $"[link]{url}[/]");
        }

        return text;
    }
}
