using Spectre.Console;

namespace ConsoleNotes;

internal class Note
{
    public string Title { get; set; }
    public string Body { get; set; }

    public Note(string title, string body)
    {
        Title = title;
        Body = body;
    }

    public Spectre.Console.Panel GetAsPanel()
    {
        Panel panel = new Panel(new Markup(Body));
        panel.Header = new PanelHeader(Title);
        panel.BorderStyle = new Style(ColorCycle.Next());
        return panel.Expand();
    }
}
