using Spectre.Console;

namespace ConsoleNotes;

public class Program
{
    public static List<string> Notes = new List<string>();
    private static Range displayRange = new Range();

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

        /* Wait to continue */
        Console.ReadKey(true);
        Console.Clear();

        /* Main loop */
        new KeyEvents(AllKeysEventHandler, new List<ConsoleKey> {
            ConsoleKey.DownArrow,
            ConsoleKey.UpArrow,
            ConsoleKey.N,
            ConsoleKey.D,
            ConsoleKey.E
        }).Start();
    }

    public static KeyEventHandler AllKeysEventHandler = (keyinfo) =>
    {
        ConsoleKey key = keyinfo.Key;

        if (key == ConsoleKey.DownArrow)
        {

        }
    };

    public static void Update()
    {

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