using Spectre.Console;
using System.Data;
using System.Text.RegularExpressions;

namespace ConsoleNotes;

public class Program
{
    private static List<string> Notes;
    private static NotesRange displayRange;
    private static Mode mode = Mode.ViewNotes;
    public static bool KeyEventListenerPaused = false;

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
            ConsoleKey.E
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
            Console.SetCursorPosition(0, 0);
        } else if (key == ConsoleKey.V)
        {
            // Switch to view mode
            UpdateMode(Mode.ViewNotes);
        } else if (key == ConsoleKey.N)
        {
            // Switch to NewNote mode
            UpdateMode(Mode.NewNote);
        } else if (key == ConsoleKey.E)
        {
            UpdateMode(Mode.EditNote);
        } else if (key == ConsoleKey.D)
        {
            UpdateMode(Mode.DeleteNote);
        }
    };

    public static void Update()
    {
        Console.Clear();
        Console.SetCursorPosition(0, 0);

        if (mode == Mode.ViewNotes)
        {
            // Hide cursor
            Console.CursorVisible = false;

            // In any given range (s - e), the range starts at s and ends at s + count. count = (e - s + 1)
            List<string> notes = Notes.GetRange(displayRange.Start, displayRange.End - displayRange.Start + 1);

            for (int i = 0; i < notes.Count; i++)
            {
                AnsiConsole.Write(new Panel(new Text(notes[i])).Expand());
            }
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

            string title = "{{TITLE_EMPTY}}";

            if (add_title)
            {
                title = AnsiConsole.Ask<string>("[yellow]Title your [deeppink3]note[/]:[/]");
            }

            CreateNote(title);
        }
    }

    internal static void UpdateMode(Mode m)
    {
        mode = m;
        Update();
    }

    public static void SaveNotes()
    {
        string notes = string.Join("{<sep>}", Notes) + "{<sep>}";
        File.WriteAllText(@"C:\ConsoleNotes\notes.txt", notes);
    }

    public static void CreateNote(string title)
    {
        KeyEventListenerPaused = true;

        char[] note = new char[10000];
        int len = 0;
        int c = 0;

        while (true)
        {
            ConsoleKeyInfo keyinfo = Console.ReadKey(true);

            if (keyinfo.Key == ConsoleKey.Enter)
            {
                // Pressing Enter creates the note
                if (len > 0)
                {
                    break;
                }
            }
            else if (keyinfo.Key == ConsoleKey.Backspace || keyinfo.Key == ConsoleKey.Delete)
            {
                // No chars to delete
                if (c == 0) continue;

                // Delete last char
                note[--c] = ' ';
                len--;

                // Rewrite contents to screen
                Console.SetCursorPosition(0, 1);
                Console.Write(note);
                Console.CursorLeft--;
                note[c] = '\0';
                continue;
            }
            else if (keyinfo.Key == ConsoleKey.LeftArrow)
            {
                c--;
                Console.CursorLeft--;
                continue;
            }
            else if (keyinfo.Key == ConsoleKey.RightArrow && c < len)
            {
                c++;
                Console.CursorLeft++;
            }
            else if (((int)keyinfo.Key < 48 || (int)keyinfo.Key > 111) || keyinfo.Key == ConsoleKey.LeftWindows || keyinfo.Key == ConsoleKey.RightWindows || keyinfo.Key == ConsoleKey.Applications || keyinfo.Key == ConsoleKey.Sleep)
            {
                continue;
            }

            Console.Write(keyinfo.KeyChar);
            note[c] = keyinfo.KeyChar;
            len++;
            c++;
        }

        char[] chars = new char[len];
        for (int i = 0; i < chars.Length; i++)
        {
            chars[i] = note[i];
        }

        // Create the note
        Notes.Add(new string(chars).Trim());
        SaveNotes();

        // Move range to account for new note
        displayRange.Start++;
        displayRange.End++;

        // Switch to view the new note
        UpdateMode(Mode.ViewNotes);
        KeyEventListenerPaused = false;
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