using Spectre.Console;
using System.Text.RegularExpressions;

namespace ConsoleNotes;

public class Note
{
    public static string NoteSeparator = "{<@SEP>}";
    public static string TitleSeparator = "{<@TITLE>}";
    private static string EmptyTitle = "{{TITLE_EMPTY}}";
    private bool NoTitle { get; set; }
    public string Title { get; set; }
    public string Body { get; set; }

    public Note(string title, string body)
    {
        Title = title;
        Body = body;
        NoTitle = (Title == EmptyTitle);
    }

    public Note(string raw_note_content)
    {
        string[] note = raw_note_content.Split(TitleSeparator);
        Title = note[0];
        Body = note[1];
        NoTitle = (Title == EmptyTitle);
    }

    private Spectre.Console.Panel GetAsPanel()
    {
        Panel panel = new Panel(new Markup(Body));
        if (!NoTitle) panel.Header = new PanelHeader(Title);
        panel.BorderStyle = new Style(ColorCycle.Next());
        return panel.Expand();
    }

    public void Display()
    {
        AnsiConsole.Write(GetAsPanel());
    }

    public static string ParseLinksMarkup(string text)
    {
        var regex = new Regex(@"(https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|www\.[a-zA-Z0-9][a-zA-Z0-9-]+[a-zA-Z0-9]\.[^\s]{2,}|https?:\/\/(?:www\.|(?!www))[a-zA-Z0-9]+\.[^\s]{2,}|www\.[a-zA-Z0-9]+\.[^\s]{2,})");

        foreach (Match match in regex.Matches(text))
        {
            string url = match.Value;
            text = text.Replace(url, $"[link]{url}[/]");
        }
        
        return text;
    }
}
