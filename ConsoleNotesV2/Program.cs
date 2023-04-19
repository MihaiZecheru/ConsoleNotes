﻿using Spectre.Console;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace ConsoleNotes;

public class Program
{
    public static UserSettings Settings;
    private static List<Note> Notes;
    private static NotesRange displayRange;
    private static int displayRangeCount = 10;
    private static bool NotesOrderNewestFirst = true;
    private static Mode mode = Mode.ViewNotes;
    public static bool KeyEventListenerPaused = false;

    // MessageBox for throwing exceptions after Ctrl+S (save note) fails due to a markup syntax error
    [DllImport("User32.dll", CharSet = CharSet.Unicode)]
    public static extern int MessageBox(IntPtr h, string m, string c, int type);

    public static void Main()
    {
        /* Setup notes file */
        string notesFilepath = @"C:\ConsoleNotes\notes.txt";


        if (!File.Exists(notesFilepath))
        {
            Directory.CreateDirectory(@"C:\ConsoleNotes");
            File.Create(notesFilepath);
        }

        /* Get settings from file */
        Settings = new UserSettings();

        /* Setup console */
        Console.Title = "Console Notes";
        Console.CursorVisible = false;
        Console.Clear();
        AnsiConsole.Write(new FigletText("ConsoleNotes").Centered().Color(ConsoleColor.DarkCyan));

        /* Load notes */
        string notes = File.ReadAllText(notesFilepath);
        List<string> raw_notes = notes.Split(Note.NoteSeparator).ToList();
        if (raw_notes[raw_notes.Count - 1].Length == 0)
            raw_notes.RemoveAt(raw_notes.Count - 1);
        Notes = raw_notes.Select(n => new Note(n)).ToList();

        /* Show notes */
        displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);
        NotesOrderNewestFirst = Settings.NotesDisplayOrder_NewestFirst;

        /* Wait to continue */
        Console.ReadKey(true);

        /* Start App */
        Update();

        /* Main loop */
        new KeyEvents(AllKeysEventHandler, new List<ConsoleKey> {
            // Move view range down once (up the list)
            ConsoleKey.DownArrow,
            
            // Move view range up once (down the list)
            ConsoleKey.UpArrow,
            
            // Reverse the order the notes are displayed in - oldest first or newest first
            ConsoleKey.R,
            
            // Move view range to the very top (home, beginning of the list, index 0)
            ConsoleKey.Q,
            
            // Move view range to the very bottom (end, end of the list)
            ConsoleKey.A,
            
            // Set help mode (open the help menu)
            ConsoleKey.H,

            // Set EditSettings mode
            ConsoleKey.S,
            
            // Set ViewNotes mode
            ConsoleKey.V,
            
            // Set NewNote mode
            ConsoleKey.N,
            
            // Set DeleteNote mode
            ConsoleKey.D,
            
            // Set EditNote mode
            ConsoleKey.E
        }).Start();
    }

    public static KeyEventHandler AllKeysEventHandler = (keyinfo) =>
    {
        ConsoleKey key = keyinfo.Key;

        // Shift the viewing range down the screen by one (up once)
        if (key == ConsoleKey.DownArrow && mode == Mode.ViewNotes)
        {
            bool change_made = false;
            if (NotesOrderNewestFirst)
            {
                // Bounds check
                if (displayRange.End != Notes.Count - 1)
                {
                    displayRange.Start++;
                    displayRange.End++;
                    change_made = true;
                }
            }
            else
            {
                // Bounds check
                if (displayRange.Start != 0)
                {
                    displayRange.Start--;
                    displayRange.End--;
                    change_made = true;
                }
            }

            if (change_made) Update();
        }
        // Shift the viewing range up the screen by one (down once)
        else if (key == ConsoleKey.UpArrow && mode == Mode.ViewNotes)
        {
            bool change_made = false;
            if (NotesOrderNewestFirst)
            {
                // Bounds check
                if (displayRange.Start != 0)
                {
                    displayRange.Start--;
                    displayRange.End--;
                    change_made = true;
                }
            }
            else
            {
                // Bounds check
                if (displayRange.End != Notes.Count - 1)
                {
                    displayRange.Start++;
                    displayRange.End++;
                    change_made = true;
                }
            }

            if (change_made)
            {
                Update();
                Console.SetCursorPosition(0, 0);
            }
        }
        // Reverse the order the notes are displayed in - oldest first or newest first
        else if (key == ConsoleKey.R && mode == Mode.ViewNotes)
        {
            if (NotesOrderNewestFirst)
            {
                NotesOrderNewestFirst = false;
            }
            else
            {
                NotesOrderNewestFirst = true;
            }

            Update();
        }
        // Switch to ViewNotes mode
        else if (key == ConsoleKey.V)
        {
            UpdateMode(Mode.ViewNotes);
        }
        // Switch to NewNote mode
        else if (key == ConsoleKey.N)
        {
            UpdateMode(Mode.NewNote);
        }
        // Switch to EditNote mode
        else if (key == ConsoleKey.E)
        {
            UpdateMode(Mode.EditNote);
        }
        // Switch to DeleteNote mode
        else if (key == ConsoleKey.D)
        {
            UpdateMode(Mode.DeleteNote);
        }
        // Switch to Help mode (open the help menu)
        else if (key == ConsoleKey.H)
        {
            UpdateMode(Mode.Help);
        }
        // Move view range to the very top (home, beginning of the list, index 0)
        else if (key == ConsoleKey.Q)
        {
            displayRange = new NotesRange(0, displayRangeCount);
            Update();
            Console.SetCursorPosition(0, 0);
        }
        // Move view range to the very bottom (end, end of the list)
        else if (key == ConsoleKey.A)
        {
            displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);
            Update();
        }
        // Switch to EditSettings mode
        else if (key == ConsoleKey.S)
        {
            UpdateMode(Mode.EditSettings);
        }
    };

    /// <summary>
    /// update the screen
    /// </summary>
    public static void Update()
    {
        Console.Clear();

        if (mode == Mode.ViewNotes)
        {
            // Hide cursor
            Console.CursorVisible = false;

            /***
             * If the user is using Windows Terminal instead of powershell or a command prompt,
             * a bug appears with the display text. When clearing the screen to rewrite the notes (using the new displayRange),
             * the console still remains scrollable so that the user can scroll up and see the old range that should have been deleted.
             * 
             * I found this trick that works in detecting the terminal used - the Console.Title doesn't show for the Window Terminal
             * as the title is usually set to the CD
             ***/
            if (Console.Title != "Console Notes")
            {
                // This character will clear the scroll of the terminal
                Console.Write("\x1b[3J");
                Console.SetCursorPosition(0, 0);
            }

            if (Notes.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]You have no notes. Press [deeppink3]N[/] to create a new note or [deeppink3]H[/] to view the help menu[/]");
                return;
            }

            // In any given range (s - e), the range starts at s and ends at s + count. count = (e - s + 1)
            List<Note> notes = Notes.GetRange(displayRange.Start, displayRange.End - displayRange.Start + 1);
            if (!NotesOrderNewestFirst)
            {
                notes.Reverse();
            }

            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].Display();
                Console.WriteLine();
            }
        }
        else if (mode == Mode.NewNote)
        {
            var rule = new Spectre.Console.Rule("[deeppink3]Write A New Note[/]");
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
                
                // Clear the screen
                Console.SetCursorPosition(0, 1);
                Console.Write(new string(' ', title.Length + 17));
                Console.SetCursorPosition(0, 1);
            }

            CreateNote(title);
        }
        else if (mode == Mode.EditSettings)
        {
            var rule = new Spectre.Console.Rule("[deeppink3]Current ConsoleNotes Settings[/]");
            rule.Style = new Style(Color.Yellow);
            AnsiConsole.Write(rule);
            Console.WriteLine();

            // Rainbow notes
            Console.WriteLine($"Show Rainbow Notes:\t{Settings.ShowRainbowNotes}");
            
            // Display order
            if (Settings.NotesDisplayOrder_NewestFirst)
            {
                Console.WriteLine("Display Order:\t\tNewest First");
            }
            else
            {
                Console.WriteLine("Display Order:\t\tOldest First");
            }

            // Backups
            Console.WriteLine($"Create Backups:\t\t{Settings.CreateBackups}\n");

            // Date Format
            if (Settings.DateDayFirst)
            {
                Console.WriteLine("Date Format:\t\tDD/MM/YYYY");
            }
            else
            {
                Console.WriteLine("Date Format:\t\tMM/DD/YYYY");
            }

            // Colors
            Console.WriteLine($"Color1: #{Settings.Color1}\t\tColor6: #{Settings.Color6}");
            Console.WriteLine($"Color2: #{Settings.Color2}\t\tColor7: #{Settings.Color7}");
            Console.WriteLine($"Color3: #{Settings.Color3}\t\tColor8: #{Settings.Color8}");
            Console.WriteLine($"Color4: #{Settings.Color4}\t\tColor9: #{Settings.Color9}");
            Console.WriteLine($"Color5: #{Settings.Color5}\t\tColor0: #{Settings.Color0}");

            // Bottom rule
            Console.WriteLine();
            rule.Title = "[deeppink3]Change ConsoleNotes Settings[/]";
            AnsiConsole.Write(rule);
            Console.WriteLine();

            // Ask user which setting they want to change
            var change_setting_prompt = new SelectionPrompt<string>()
                .Title("[yellow]Which [deeppink3]setting[/] do you want to change?[/]")
                .AddChoices(new[] {
                    "Show Rainbow Notes",
                    "Display Order",
                    "Create Backups",
                    "Date Format",
                    "Color1", "Color2", "Color3",
                    "Color4", "Color5", "Color6",
                    "Color7", "Color8", "Color9", "Color0",
                    "Exit"
                })
                .HighlightStyle(new Style(Color.DeepPink3));

            change_setting_prompt.DisabledStyle = new Style(Color.Yellow);
            string change_setting = AnsiConsole.Prompt(change_setting_prompt);

            var regex = new Regex(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$");
            Func<int, string> ChangeColorTo = (int color_num) =>
            {
                string new_color = AnsiConsole.Ask<string>($"[yellow]Enter hex for [deeppink3]Color{color_num}[/]:[/]");
                if (regex.IsMatch(new_color))
                {
                    // Remove #
                    return new_color.Substring(1);
                }
                else
                {
                    // Alert user the update failed
                    MessageBox((IntPtr)0, $"The hex you entered ({new_color}) was invalid\n\nNote: the hex must begin with a #", "Invalid Color Hex", 0);
                    return string.Empty;
                }
            };

            Func<string, bool> AskUserTrueOrFalse = (string question) =>
            {
                var set_new_value_prompt = new SelectionPrompt<string>()
                    .Title($"[yellow]Set new value for: [deeppink3]{question}[/][/]")
                    .AddChoices(new[] { "True", "False" })
                    .HighlightStyle(new Style(Color.DeepPink3));

                set_new_value_prompt.DisabledStyle = new Style(Color.Yellow);
                return AnsiConsole.Prompt(set_new_value_prompt) == "True";
            };

            string hex;
            switch (change_setting)
            {
                case "Exit":
                    UpdateMode(Mode.ViewNotes);
                    break;

                case "Show Rainbow Notes":
                    Settings.ShowRainbowNotes = AskUserTrueOrFalse("Show Rainbow Notes");
                    break;

                case "Display Order":
                    var display_order_new_value_prompt = new SelectionPrompt<string>()
                        .Title("[yellow]Set new value for: [deeppink3]Display Order[/][/]")
                        .AddChoices(new[] { "Newest First", "Oldest First" })
                        .HighlightStyle(new Style(Color.DeepPink3));

                    display_order_new_value_prompt.DisabledStyle = new Style(Color.Yellow);
                    Settings.NotesDisplayOrder_NewestFirst = AnsiConsole.Prompt(display_order_new_value_prompt) == "Newest First";
                    break;

                case "Create Backups":
                    Settings.CreateBackups = AskUserTrueOrFalse("Create Backups");
                    break;

                case "Date Format":
                    var date_day_first_new_value_prompt = new SelectionPrompt<string>()
                        .Title("[yellow]Set new value for: [deeppink3]Date Format[/][/]")
                        .AddChoices(new[] { "MM/DD/YYYY", "DD/MM/YYYY" })
                        .HighlightStyle(new Style(Color.DeepPink3));

                    date_day_first_new_value_prompt.DisabledStyle = new Style(Color.Yellow);
                    Settings.DateDayFirst = AnsiConsole.Prompt(date_day_first_new_value_prompt) == "DD/MM/YYYY";
                    break;

                case "Color1":
                    hex = ChangeColorTo(1);
                    if (hex== string.Empty) Update();
                    Settings.Color1 = hex;
                    break;

                case "Color2":
                    hex = ChangeColorTo(2);
                    if (hex == string.Empty) Update();
                    Settings.Color2 = hex;
                    break;

                case "Color3":
                    hex = ChangeColorTo(3);
                    if (hex == string.Empty) Update();
                    Settings.Color3 = hex;
                    break;

                case "Color4":
                    hex = ChangeColorTo(4);
                    if (hex == string.Empty) Update();
                    Settings.Color4 = hex;
                    break;

                case "Color5":
                    hex = ChangeColorTo(5);
                    if (hex == string.Empty) Update();
                    Settings.Color5 = hex;
                    break;

                case "Color6":
                    hex = ChangeColorTo(6);
                    if (hex == string.Empty) Update();
                    Settings.Color6 = hex;
                    break;

                case "Color7":
                    hex = ChangeColorTo(7);
                    if (hex == string.Empty) Update();
                    Settings.Color7 = hex;
                    break;

                case "Color8":
                    hex = ChangeColorTo(8);
                    if (hex == string.Empty) Update();
                    Settings.Color8 = hex;
                    break;

                case "Color9":
                    hex = ChangeColorTo(9);
                    if (hex == string.Empty) Update();
                    Settings.Color9 = hex;
                    break;

                case "Color0":
                    hex = ChangeColorTo(0);
                    if (hex == string.Empty) Update();
                    Settings.Color0 = hex;
                    break;
            }

            // Refresh to show changes
            Update();
        }
        else if (mode == Mode.EditNote)
        {
            int selected_note_index = GetSelectedNoteIndex();
            if (selected_note_index == -1) return;
            Note selected_note = Notes[selected_note_index];

            while (true)
            {
                var rule = new Spectre.Console.Rule("[deeppink3]Edit Note[/]");
                rule.Style = new Style(Color.Yellow);
                AnsiConsole.Write(rule);

                var what_to_edit_prompt = new SelectionPrompt<string>()
                    .Title("[yellow]Choose a field to [deeppink3]edit[/][/]")
                    .AddChoices(new[] { "Quit", "Title", "Body" })
                    .HighlightStyle(new Style(Color.DeepPink3));

                what_to_edit_prompt.DisabledStyle = new Style(Color.Yellow);
                var answer = AnsiConsole.Prompt(what_to_edit_prompt);

                if (answer == "Title")
                {
                    Console.Clear();
                    string old_title = selected_note.Title == Note.EmptyTitle ? "<No Title>" : selected_note.Title;
                    AnsiConsole.Write(new Markup($"[deeppink3]Current Title:[/] [yellow]{old_title}[/]\n\n"));

                    string new_title = AnsiConsole.Ask<string>("[yellow]Give your [deeppink3]note[/] a new title[/]", "press enter to remove title");
                    if (new_title == "press enter to remove title") new_title = Note.EmptyTitle;
                    EditNote(selected_note_index, new Note(new_title, selected_note.Body, selected_note.IsJson));
                }
                else if (answer == "Body")
                {
                    bool _isJson = selected_note.IsJson;
                    string updated_note_body = string.Empty;
                    bool user_finished_editing_note = new Editor(selected_note).MainLoop(ref updated_note_body, ref _isJson);

                    if (!user_finished_editing_note) break;
                    EditNote(selected_note_index, new Note(selected_note.Title, updated_note_body, _isJson));
                    break;
                }
                else break;
                Console.Clear();
            }

            UpdateMode(Mode.ViewNotes);
        }
        else if (mode == Mode.DeleteNote)
        {
            int selected_note_index = GetSelectedNoteIndex();
            if (selected_note_index == -1) return;
            Note selected_note = Notes[selected_note_index];
            /** Show preview of selected note **/
            
            // Move past where the prompt will appear
            Console.SetCursorPosition(0, 5);

            // Write the selected note to the console
            selected_note.Display();

            // Move cursor back to beginning
            Console.SetCursorPosition(0, 0);

            // Write prompt
            var confirmation_prompt = new SelectionPrompt<string>()
                .Title($"[yellow]Are you sure you want to delete note [deeppink3]#{Math.Abs(selected_note_index - Notes.Count)}[/]?[/]")
                .AddChoices(new[] { "Yes", "No" })
                .HighlightStyle(new Style(Color.DeepPink3));

            confirmation_prompt.DisabledStyle = new Style(Color.Yellow);
            bool delete = AnsiConsole.Prompt(confirmation_prompt) == "Yes";

            if (!delete)
            {
                UpdateMode(Mode.ViewNotes);
            }
            else
            {
                DeleteNote(selected_note_index);
                UpdateMode(Mode.ViewNotes);
            }
        }
    }

    /// <summary>
    /// Change the value of <see cref="mode"/>
    /// </summary>
    /// <param name="m"></param>
    internal static void UpdateMode(Mode m)
    {
        mode = m;
        Update();
    }

    /// <summary>
    /// Save all notes to the notes.txt file
    /// </summary>
    public static void SaveNotes()
    {
        string notes = string.Join(Note.NoteSeparator, Notes) + Note.NoteSeparator;
        File.WriteAllText(@"C:\ConsoleNotes\notes.txt", notes);
    }

    /// <summary>
    /// Create a new note, add it to <see cref="Notes"/>, then save to file
    /// </summary>
    /// <param name="title">The title of the note</param>
    public static void CreateNote(string title)
    {
        KeyEventListenerPaused = true;

        // Get the note body & isJson value
        string body = string.Empty;
        bool isJson = false;
        bool user_finished_writing_note = new Editor().MainLoop(ref body, ref isJson);
        
        if (!user_finished_writing_note)
        {
            // Switch to view mode
            UpdateMode(Mode.ViewNotes);
            KeyEventListenerPaused = false;
            return;
        }

        // Create then save the note
        Notes.Add(new Note(title, body, isJson));
        SaveNotes();

        // Move range to very end to account for new note
        displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);

        // Switch to view the new note
        UpdateMode(Mode.ViewNotes);
        KeyEventListenerPaused = false;
    }

    /// <summary>
    /// Delete an existing note
    /// </summary>
    /// <param name="note_index">The index of the note to delete in the <see cref="Notes"/> list</param>
    public static void DeleteNote(int note_index)
    {
        Notes.RemoveAt(note_index);
        /* Set range to show from beginning */
        displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);
        SaveNotes();
    }

    /// <summary>
    /// Edit an existing note then save to file
    /// </summary>
    /// <param name="original_note_index">The index of the original note in the <see cref="Notes"/> list</param>
    /// <param name="newNote">The note to replace the original with</param>
    public static void EditNote(int original_note_index, Note newNote)
    {
        Notes[original_note_index] = newNote;
        SaveNotes();
    }

    /// <summary>
    /// Have the user select a note then return the index of that note in the <see cref="Notes"/> list
    /// </summary>
    private static int GetSelectedNoteIndex()
    {
        List<string> choices = new List<string>() { "Quit" };

        // Reverse notes to show the most recent ones at the top
        IEnumerable<Note> notes_sorted = NotesOrderNewestFirst ? Notes.ToArray().Reverse() : Notes;
        IEnumerable<string> parsedNotes = notes_sorted.Select((note, index) =>
        {
            string date = note.CreatedAt;
            string date_only = date.Substring(0, date.IndexOf(' '));

            if (note.Title == Note.EmptyTitle)
                return $"{index + 1}. {date_only} | <No Title>";
            else
                return $"{index + 1}. {date_only} | {note.Title}";
        });
        choices.AddRange(parsedNotes);

        var select_note_prompt = new SelectionPrompt<string>()
            .Title("[yellow]Select a note to [deeppink3]edit[/][/]")
            .AddChoices(choices)
            .HighlightStyle(new Style(Color.DeepPink3));

        select_note_prompt.DisabledStyle = new Style(Color.Yellow);
        string selected_option = AnsiConsole.Prompt(select_note_prompt);
        
        if (selected_option == "Quit")
        {
            UpdateMode(Mode.ViewNotes);
            return -1;
        }

        int note_index = Convert.ToInt32(selected_option.Substring(0, selected_option.IndexOf(".")));
        
        // Notes were reversed earlier in the function, so the index needs to be reversed back
        if (NotesOrderNewestFirst)
            return Notes.Count - note_index;
        else
            return note_index - 1;
    }
}