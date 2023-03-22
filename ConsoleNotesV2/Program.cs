using Spectre.Console;
using System.Data;
using System.Text.RegularExpressions;

namespace ConsoleNotes;

public class Program
{
    private static List<string> Notes;
    private static NotesRange displayRange;
    private static Mode mode = Mode.View;

    public static void Main()
    {
        /* Setup notes file */
        string notesFilepath = @"C:\ConsoleNotes\notes.txt";

        if (!File.Exists(notesFilepath))
        {
            Directory.CreateDirectory(@"C:\ConsoleNotes");
            File.Create(notesFilepath);
        }

        /* Setup console */
        Console.Title = "Console Notes";
        Console.CursorVisible = false;
        AnsiConsole.Write(new FigletText("ConsoleNotes").Centered().Color(ConsoleColor.DarkCyan));

        /* Load notes */
        string notes = File.ReadAllText(notesFilepath);
        Notes = notes.Split("{<sep>}").ToList();
        if (Notes[Notes.Count - 1].Length == 0)
            Notes.RemoveAt(Notes.Count - 1);

        /* Show most recent notes */
        displayRange = new NotesRange((Notes.Count - 11 > 0) ? Notes.Count - 11 : 0, Notes.Count - 1);

        /* Wait to continue */
        Console.ReadKey(true);

        /* Start App */
        Update();

        /* Main loop */
        new KeyEvents(AllKeysEventHandler, new List<ConsoleKey> {
            ConsoleKey.DownArrow,
            ConsoleKey.UpArrow,
            ConsoleKey.V,
            ConsoleKey.N,
            ConsoleKey.D,
            ConsoleKey.E,
            ConsoleKey.Enter,
        }).Start();
    }

    public static KeyEventHandler AllKeysEventHandler = (keyinfo) =>
    {
        ConsoleKey key = keyinfo.Key;

        if (key == ConsoleKey.DownArrow && displayRange.End != Notes.Count - 1 && mode == Mode.ViewNotes)
        {
            displayRange.Start++;
            displayRange.End++;
            Update();
        } else if (key == ConsoleKey.UpArrow && displayRange.Start != 0 && mode == Mode.ViewNotes)
        {
            displayRange.Start--;
            displayRange.End--;
            Update();
        } else if (key == ConsoleKey.V)
        {
            // Switch to view mode
            Console.CursorVisible = false;
            mode = Mode.ViewNotes;
            Update();
        } else if (key == ConsoleKey.N)
        {
            // Switch to NewNote mode
            mode = Mode.NewNote;
            Update();
        } else if (key == ConsoleKey.E)
        {
            mode = Mode.EditNote;
            Update();
        } else if (key == ConsoleKey.D)
        {
            mode = Mode.DeleteNote;
            Update();
        } else if (key == ConsoleKey.Enter)
        {
            if (mode == Mode.WritingNote)
            {

            }
        }
    };

    public static void Update()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);

        if (mode == Mode.ViewNotes)
        {
            // In any given range (s - e), the range starts at s and ends at s + count. count = (e - s + 1)
            List<string> notes = Notes.GetRange(displayRange.Start, displayRange.End - displayRange.Start + 1);

            for (int i = 0; i < notes.Count; i++)
            {
                AnsiConsole.Write(new Panel(new Text(notes[i])).Expand());
            }

            // Show the new note panel
            Console.SetCursorPosition(0, 0);
        } else if (mode == Mode.NewNote)
        {
            var rule = new Spectre.Console.Rule("[deeppink3]Create New Note[/]");
            rule.Style = new Style(Color.Yellow);
            AnsiConsole.Write(rule);

            var add_title_prompt = new SelectionPrompt<string>()
                .Title("[yellow]Give your note a [deeppink3]title[/]?[/]")
                .AddChoices(new[] { "Yes", "No" })
                .HighlightStyle(new Style(Color.DeepPink3));

            add_title_prompt.DisabledStyle = new Style(Color.Yellow);
            bool add_title = AnsiConsole.Prompt(add_title_prompt) == "Yes";
            
            if (add_title)
            {
                string title = AnsiConsole.Ask<string>("[yellow]Title your [deeppink3]note[/]:[/]");
                Console.WriteLine(title)    ;
            }

            mode = Mode.WritingNote;
        }
    }

    public static void SaveNotes()
    {
        string notes = string.Join("{<sep>}", Notes);
        File.WriteAllText(@"C:\ConsoleNotes\notes.txt", notes);
        Update();
    }

    public static void DeleteNote(string note)
    {
        Notes.RemoveAt(Notes.IndexOf(note));
        SaveNotes();
    }

    public static void EditNote(string note, string newNote)
    {
        Notes[Notes.IndexOf(note)] = newNote;
        SaveNotes();
    }
}