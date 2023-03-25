using Spectre.Console;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace ConsoleNotes;

public class Note
{
    private static Regex LinksRegex = new Regex(@"(https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,})");

    /// <summary>
    /// Used to separate notes in the notes.txt file
    /// </summary>
    public static string NoteSeparator = "{<@SEP>}";

    /// <summary>
    /// Use to separate the title of a note from the body, with the title appearing on the left side of the separator
    /// </summary>
    public static string TitleSeparator = "{<@TITLE>}";

    /// <summary>
    /// The string used to represent a note without a title
    /// </summary>
    private static string EmptyTitle = "{{TITLE_EMPTY}}";

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
    /// For making a new note that doesn't already exist
    /// </summary>
    /// <param name="title">The title of the note</param>
    /// <param name="body">The content of the note</param>
    public Note(string title, string body)
    {
        Title = title;
        Body = body;
        NoTitle = (Title == EmptyTitle);
    }

    /// <summary>
    /// For turning the raw text of a note from the notes.txt file and creating a Note object from it
    /// </summary>
    /// <param name="raw_note_content">The raw text from the notes.txt file, which still includes a title separator</param>
    public Note(string raw_note_content)
    {
        string[] note = raw_note_content.Split(TitleSeparator);
        Title = note[0];
        Body = note[1];
        NoTitle = (Title == EmptyTitle);
    }

    /// <summary>
    /// Turn the note into a Spectre.Console.Panel, which can be displayed to the screen with AnsiConsole.Write()
    /// </summary>
    /// <returns>A Spectre.Console.Panel to display on the console</returns>
    /// <remarks>The panel's border will be given the next color from the ColorCycle</remarks>
    private Spectre.Console.Panel GetAsPanel()
    {
        Panel panel = new Panel(new Markup(Body));
        if (!NoTitle) panel.Header = new PanelHeader(Title);
        panel.BorderStyle = new Style(ColorCycle.Next());
        return panel.Expand();
    }

    /// <summary>
    /// Write the note to the console
    /// </summary>
    public void Display()
    {
        AnsiConsole.Write(GetAsPanel());
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
        return $"{Title}{TitleSeparator}{Body}";
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
