using Spectre.Console;
using System.Text.RegularExpressions;

namespace ConsoleNotes;

public class Program
{
    private static List<Note> Notes;
    private static NotesRange displayRange;
    private static int displayRangeCount = 10;
    private static Mode mode = Mode.ViewNotes;
    public static bool KeyEventListenerPaused = false;
    private static int TAB_SIZE = 4;

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
        Console.Clear();
        AnsiConsole.Write(new FigletText("ConsoleNotes").Centered().Color(ConsoleColor.DarkCyan));

        /* Load notes */
        string notes = File.ReadAllText(notesFilepath);
        List<string> raw_notes = notes.Split(Note.NoteSeparator).ToList();
        if (raw_notes[raw_notes.Count - 1].Length == 0)
            raw_notes.RemoveAt(raw_notes.Count - 1);
        Notes = raw_notes.Select(n => new Note(n)).ToList();

        /* Show most recent notes */
        displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);

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
            // Move view range to the very top (home, beginning of the list, index 0)
            ConsoleKey.H,
            // Move view range to the very bottom (end, end of the list)
            ConsoleKey.W,
            // Set view mode
            ConsoleKey.V,
            // Set new note mode
            ConsoleKey.N,
            // Set delete mode
            ConsoleKey.D,
            // Set edit mode
            ConsoleKey.E
        }).Start();
    }

    public static KeyEventHandler AllKeysEventHandler = (keyinfo) =>
    {
        ConsoleKey key = keyinfo.Key;

        // Shift the viewing range down the screen by one (up once)
        if (key == ConsoleKey.DownArrow && displayRange.End != Notes.Count - 1 && mode == Mode.ViewNotes)
        {
            displayRange.Start++;
            displayRange.End++;
            Update();
        }
        // Shift the viewing range up the screen by one (down once)
        else if (key == ConsoleKey.UpArrow && displayRange.Start != 0 && mode == Mode.ViewNotes)
        {
            displayRange.Start--;
            displayRange.End--;
            Update();
            Console.SetCursorPosition(0, 0);
        }
        // Switch to view mode
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
        // Move view range to the very top (home, beginning of the list, index 0)
        else if (key == ConsoleKey.H)
        {
            displayRange = new NotesRange(0, displayRangeCount);
            Update();
            Console.SetCursorPosition(0, 0);
        }
        // Move view range to the very bottom (end, end of the list)
        else if (key == ConsoleKey.W)
        {
            displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);
            Update();
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
            List<Note> notes = Notes.GetRange(displayRange.Start, displayRange.End - displayRange.Start + 1);

            for (int i = 0; i < notes.Count; i++)
            {
                notes[i].Display();
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
    }

    internal static void UpdateMode(Mode m)
    {
        mode = m;
        Update();
    }

    public static void SaveNotes()
    {
        string notes = string.Join(Note.NoteSeparator, Notes) + Note.NoteSeparator;
        File.WriteAllText(@"C:\ConsoleNotes\notes.txt", notes);
    }

    public static void CreateNote(string title)
    {
        KeyEventListenerPaused = true;

        // lines[cli][ci]
        List<List<char>> lines = new List<List<char>> { new List<char>() };

        // Current line index in 'note'
        int cli = 0;

        // Current index in 'note[cli]'
        int ci = 0;

        // Function for concatenating a note
        Func<int, string> GetNoteContent = (int start) =>
        {
            string s = string.Empty;
            
            for (int i = start; i < lines.Count; i++)
            {
                for (int j = 0; j < lines[i].Count; j++) s += lines[i][j];
                s += '\n';
            }

            return s.Trim();
        };

        // Keep track of changes made to the note
        CtrlZ states = new CtrlZ();

        // Loop for getting user input
        while (true)
        {
            ConsoleKeyInfo keyinfo = Console.ReadKey(true);

            // Create then save the note - Ctrl+S
            if (keyinfo.Key == ConsoleKey.S && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                // TODO: exceptions & popup if markuop doesnt compile
                if (GetNoteContent(0).Length > 0) break;
                else continue;
            }
            // Undo - Ctrl+Z
            else if (keyinfo.Key == ConsoleKey.Z && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                // Get previous state
                (lines, (int left, int top)) = states.Undo();

                /*** Rewrite screen ***/

                // Clear
                Console.Clear();
                Console.SetCursorPosition(0, 0);

                // Rewrite the horizontal rule
                var rule = new Spectre.Console.Rule("[deeppink3]Write A New Note[/]");
                rule.Style = new Style(Color.Yellow);
                AnsiConsole.Write(rule);

                // Rewrite the note
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = 0; j < lines[i].Count; j++)
                    {
                        Console.WriteLine(lines[i][j]);
                    }
                }

                // Move cursor to the correct position
                ci = left;
                cli = top;
                Console.SetCursorPosition(left, top);
            }
            // Redo - Ctrl+Y
            else if (keyinfo.Key == ConsoleKey.Y && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                // Get following state
                (lines, (int left, int top)) = states.Redo();

                /*** Rewrite screen ***/

                // Clear
                Console.Clear();
                Console.SetCursorPosition(0, 0);

                // Rewrite the horizontal rule
                var rule = new Spectre.Console.Rule("[deeppink3]Write A New Note[/]");
                rule.Style = new Style(Color.Yellow);
                AnsiConsole.Write(rule);

                // Rewrite the note
                for (int i = 0; i < lines.Count; i++)
                {
                    for (int j = 0; j < lines[i].Count; j++)
                    {
                        Console.WriteLine(lines[i][j]);
                    }
                }

                // Move cursor to the correct position
                ci = left;
                cli = top;
                Console.SetCursorPosition(left, top);
            }
            // Create new line - Enter
            else if (keyinfo.Key == ConsoleKey.Enter)
            {
                /*** Creates a new line ***/

                /***
                 * IMPORTANT: if the cursor is at the bottom of the screen
                 * do not allow any text to be written
                 * 
                 * The user must expand their console to write more
                 ***/
                if (cli == Console.BufferHeight - 3) continue;

                // Create the line only if cursor is on the last line
                if (cli == lines.Count - 1)
                {
                    lines.Add(new List<char>());
                }

                // Move the cursor down to the next line anyway
                cli++;
                ci = 0;

                // Move to next line
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                continue;
            }
            // Move to beginning of line - Home
            else if (keyinfo.Key == ConsoleKey.Home)
            {
                // Move to beginning of line
                ci = 0;
                Console.CursorLeft = 0;
                continue;
            }
            // Move to end of line - End
            else if (keyinfo.Key == ConsoleKey.End)
            {
                ci = lines[cli].Count;
                Console.CursorLeft = lines[cli].Count;
                continue;
            }
            // Ctrl+Home doesn't register as an event, so it's not included
            // Move to beginning of text - Ctrl+H & Ctrl+UpArrow
            else if ((keyinfo.Key == ConsoleKey.H || keyinfo.Key == ConsoleKey.UpArrow) && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                ci = 0;
                cli = 0;
                Console.SetCursorPosition(0, 1);
                continue;
            }
            // Ctrl+End doesn't register as an event, so it's not included
            // Move to end of text - Ctrl+E & Ctrl+DownArrow
            else if ((keyinfo.Key == ConsoleKey.E || keyinfo.Key == ConsoleKey.DownArrow) && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                cli = lines.Count - 1;
                ci = lines[cli].Count;
                Console.SetCursorPosition(ci, cli + 1);
                continue;
            }
            // Move cursor left once - LeftArrow
            // Move cursor left one word - Ctrl+LeftArrow
            else if (keyinfo.Key == ConsoleKey.LeftArrow)
            {
                // Move cursor left one word - Ctrl+LeftArrow
                if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    // If index is not at the beginning
                    if (ci > 0)
                    {
                        // If the first char is a space, skip it in the calculations
                        int adjusted_ci = (lines[cli][ci - 1] == ' ') ? ci - 1 : ci;
                        List<char> range = lines[cli].GetRange(0, adjusted_ci);

                        // If the segment contains a space
                        if (range.Contains(' '))
                        {
                            int nearest_previous_space = 0;
                            while (true)
                            {
                                int index = range.FindIndex(nearest_previous_space + 1, x => x == ' ');

                                // No new index found
                                if (index == -1) break;
                                else if (index > nearest_previous_space)
                                    nearest_previous_space = index;
                            }

                            // Move the cursor
                            int amount_to_shift = ci - nearest_previous_space;
                            Console.CursorLeft -= amount_to_shift;
                            ci -= amount_to_shift;
                        }
                        else
                        {
                            // Move to the very beginning
                            // Required because the cursor, while not being at the very end, may be somewhere inside the first word
                            // Meaning the cursor still needs to go to 0
                            Console.CursorLeft = 0;
                            ci = 0;
                        }
                    }
                    // If index **is** at the beginning
                    // Move to the end of the previous line
                    else
                    {
                        // No previous line if on the first line
                        if (cli == 0) continue;

                        int end_of_line = lines[--cli].Count;
                        Console.CursorLeft = end_of_line;
                        ci = end_of_line;
                        Console.CursorTop--;
                    }
                }
                // Move cursor left once - LeftArrow
                else
                {
                    if (ci > 0)
                    {
                        ci--;
                        Console.CursorLeft--;
                    }
                    else
                    {
                        if (cli > 0)
                        {
                            ci = lines[cli - 1].Count;
                            Console.SetCursorPosition(ci, Console.CursorTop - 1);
                            cli--;
                        }
                    }
                }

                continue;
            }
            // Move cursor right once - RightArrow
            // Move cursor right one word - Ctrl+RightArrow
            else if (keyinfo.Key == ConsoleKey.RightArrow)
            {
                // Move cursor right one word - Ctrl+RightArrow
                if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    // If not at the end of the line
                    if (ci != lines[cli].Count)
                    {
                        // If the current char and first char of the range is a space, skip it in the calculations
                        int adjusted_ci;
                        if (lines[cli][ci] == ' ') adjusted_ci = ci + 1;
                        // Prevent index access error with this part before the &&
                        else if (ci + 1 < lines[cli].Count && lines[cli][ci + 1] == ' ') adjusted_ci = ci + 2;
                        else adjusted_ci = ci;

                        List<char> range = lines[cli].GetRange(adjusted_ci, lines[cli].Count - adjusted_ci);

                        // If the segment contains a space
                        if (range.Contains(' '))
                        {
                            // Start looking from the current index
                            // Get index of space inside the range, adjusted_ci is added to be able to use the closest_following_index var as an index in the original list
                            int closest_following_space = range.IndexOf(' ') + adjusted_ci + 1;

                            int amount_to_shift = closest_following_space - ci;
                            Console.CursorLeft += amount_to_shift;
                            ci += amount_to_shift;
                        }
                        else
                        {
                            // Simply move to the end of the line
                            Console.CursorLeft = lines[cli].Count;
                            ci = lines[cli].Count;
                        }
                    }
                    // If index **is** at the end of the line
                    // Move to the end of the previous line
                    else
                    {
                        /***
                         * IMPORTANT: if the cursor is at the bottom of the screen
                         * do not allow any text to be written
                         * 
                         * The user must expand their console to write more
                         ***/
                        if (cli == Console.BufferHeight - 3) continue;

                        // Move to next line if it exists or make a new one
                        if (cli == lines.Count - 1)
                        {
                            // Create a new line
                            lines.Add(new List<char>());
                        }

                        // Move to the next line
                        ci = 0;
                        cli++;
                        Console.SetCursorPosition(0, Console.CursorTop + 1);
                    }
                }
                // Move cursor right once - RightArrow
                else
                {
                    // If not at the end of the text
                    if (ci != lines[cli].Count)
                    {
                        ci++;
                        Console.CursorLeft++;
                    }
                    // At the end of the text - must move to next line
                    else
                    {
                        /***
                         * IMPORTANT: if the cursor is at the bottom of the screen
                         * do not allow any text to be written
                         * 
                         * The user must expand their console to write more
                         ***/
                        if (cli == Console.BufferHeight - 3) continue;

                        // Move to next line if it exists or make a new one
                        if (cli == lines.Count - 1)
                        {
                            // Create a new line
                            lines.Add(new List<char>());
                        }

                        // Move to the next line
                        ci = 0;
                        cli++;
                        Console.SetCursorPosition(0, Console.CursorTop + 1);
                    }
                }

                continue;
            }
            // Move cursor down once - DownArrow
            else if (keyinfo.Key == ConsoleKey.DownArrow)
            {
                /***
                 * IMPORTANT: if the cursor is at the bottom of the screen
                 * do not allow any text to be written
                 * 
                 * The user must expand their console to write more
                 ***/
                if (cli == Console.BufferHeight - 3) continue;

                // Move to next line if it exists or make a new one then move to it
                if (cli + 1 == lines.Count)
                {
                    // Create a new line
                    lines.Add(new List<char>());
                }

                // Move to the next line
                int left_pos = (Console.CursorLeft < lines[++cli].Count) ? Console.CursorLeft : lines[cli].Count;
                ci = left_pos;
                Console.SetCursorPosition(left_pos, Console.CursorTop + 1);
                continue;
            }
            // Move cursor up once - UpArrow
            else if (keyinfo.Key == ConsoleKey.UpArrow)
            {
                // If the current line is not the first line
                if (cli > 0)
                {
                    int left_pos = (Console.CursorLeft < lines[--cli].Count) ? Console.CursorLeft : lines[cli].Count;
                    ci = left_pos;
                    Console.SetCursorPosition(left_pos, Console.CursorTop - 1);
                }
                continue;
            }
            // Delete previous char or word - Backspace & Ctrl+Backspace
            else if (keyinfo.Key == ConsoleKey.Backspace)
            {
                // Delete until the last space (one word) - Ctrl+Backspace
                if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    // If index is not at the beginning
                    if (ci > 0)
                    {
                        // If the first char is a space, skip it in the calculations
                        int adjusted_ci = (lines[cli][ci - 1] == ' ') ? ci - 1 : ci;
                        List<char> range = lines[cli].GetRange(0, adjusted_ci);
                        
                        // If the segment contains a space
                        if (range.Contains(' '))
                        {
                            int nearest_previous_space = 0;
                            while (true)
                            {
                                int index = range.FindIndex(nearest_previous_space + 1, x => x == ' ');
                                
                                // No new index found
                                if (index == -1) break;
                                else if (index > nearest_previous_space)
                                    nearest_previous_space = index;
                            }

                            // Remove the chars
                            lines[cli].RemoveRange(nearest_previous_space, adjusted_ci - nearest_previous_space);

                            // Rewrite the line
                            Console.CursorLeft = 0; //                                          clear line till end of screen
                            Console.Write(new string(lines[cli].ToArray()) + new string(' ', Console.BufferWidth - lines[cli].Count));
                            Console.CursorLeft = nearest_previous_space;
                            ci = nearest_previous_space;
                        }
                        else
                        {
                            // Clear entire line
                            Console.CursorLeft = 0;
                            Console.Write(new string(' ', Console.BufferWidth));

                            // Delete until the beginning of the line
                            lines[cli].RemoveRange(0, ci);

                            // Rewrite line
                            Console.CursorLeft = 0;
                            Console.Write(new string(lines[cli].ToArray()));
                            Console.CursorLeft = 0;
                            ci = 0;
                        }
                    }
                }
                // Delete one character - Backspace
                else
                {
                    // If index is not at the beginning
                    if (ci > 0)
                    {
                        // If the char to be deleted **is** an opening square bracket [
                        // And the char after **is** a closing bracket ]
                        // Meaning if the cursor is between the brackets [], delete both
                        if (lines[cli][ci - 1] == '[' && lines[cli][ci] == ']')
                        {
                            /** Delete both square brackets **/

                            // Remove both chars
                            lines[cli].RemoveAt(ci); // Remove closing bracket ]
                            lines[cli].RemoveAt(--ci); // Remove opening bracket [

                            // Rewrite line without chars
                            int previous_cursor_pos = Console.CursorLeft;
                            Console.CursorLeft = 0;
                            Console.Write(new string(lines[cli].ToArray()) + "  ");
                            Console.CursorLeft = previous_cursor_pos - 1;
                        }
                        else
                        {
                            lines[cli].RemoveAt(--ci);

                            // Rewrite line without char
                            int previous_cursor_pos = Console.CursorLeft;
                            Console.CursorLeft = 0;
                            Console.Write(new string(lines[cli].ToArray()) + " ");
                            Console.CursorLeft = previous_cursor_pos - 1;
                        }
                    }
                    // Index is at the beginning, so the current line must be moved forward (up the screen) by one
                    else
                    {
                        // Nowhere to go if on the first line
                        if (cli == 0) continue;

                        // Used for placing the cursor
                        int prev_line_length = lines[cli - 1].Count;

                        // Append this line to the one before it
                        lines[cli - 1].AddRange(lines[cli]);
                        
                        // Delete the line
                        lines.RemoveAt(cli--);

                        /*** Update the screen ***/
                        
                        // Remember the position to go to
                        int cursor_pos = Console.CursorTop - 1;
                        ci = prev_line_length;

                        // There is too much text to wipe, so the best move is to clear everything and rewrite
                        int[] start_rewriting_from = new int[2] { Console.CursorLeft, Console.CursorTop - 1}; // Rewrite the current line as well (top - 1)
                        Console.Write(new string(' ', (lines.Count - cli) * Console.BufferWidth));
                        Console.SetCursorPosition(start_rewriting_from[0], start_rewriting_from[1]);
                        
                        Console.Write(GetNoteContent(cli));
                        Console.SetCursorPosition(ci, cursor_pos);
                    }
                }

                continue;
            }
            // Delete next char or word - Delete & Ctrl+Delete
            else if (keyinfo.Key == ConsoleKey.Delete)
            {
                // Delete until the next space (one word) - Ctrl+Delete
                if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
                {
                    // If index is not at the end
                    if (ci != lines[cli].Count)
                    {
                        // If the current char and first char of the range is a space, skip it in the calculations
                        int adjusted_ci;
                        if (lines[cli][ci] == ' ') adjusted_ci = ci + 1;
                        // Prevent index access error with this part before the &&
                        else if (ci + 1 < lines[cli].Count && lines[cli][ci + 1] == ' ') adjusted_ci = ci + 2;
                        else adjusted_ci = ci;

                        List<char> range = lines[cli].GetRange(adjusted_ci, lines[cli].Count - adjusted_ci);
                        
                        // If the segment contains a space
                        if (range.Contains(' '))
                        {
                            // Start looking from the current index
                            // Get index of space inside the range, adjusted_ci is added to be able to use the closest_following_index var as an index in the original list
                            int closest_following_space = range.IndexOf(' ') + adjusted_ci + 1;
                            
                            // Remove the chars
                            lines[cli].RemoveRange(adjusted_ci, closest_following_space - adjusted_ci);

                            // Rewrite the line
                            int previous_cursor_pos = Console.CursorLeft;
                            Console.CursorLeft = 0; //                                          clear line till end of screen
                            Console.Write(new string(lines[cli].ToArray()) + new string(' ', Console.BufferWidth - lines[cli].Count));
                            Console.CursorLeft = previous_cursor_pos;
                            ci = previous_cursor_pos;
                        }
                        else
                        {
                            // Clear line from current pos to end
                            int previous_cursor_pos = Console.CursorLeft;
                            Console.Write(new string(' ', range.Count));
                            Console.CursorLeft = previous_cursor_pos;

                            // Delete until the beginning of the line
                            lines[cli].RemoveRange(ci, lines[cli].Count - ci);

                            // Rewrite end of line
                            Console.Write(new string(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray()) + new string(' ', range.Count));
                            Console.CursorLeft = previous_cursor_pos;
                        }
                    }
                }
                // Delete one character - Delete
                else
                {
                    // If at the end of the line the next line must be appended to this one (deleting the \n that separates the two)
                    if (ci == lines[cli].Count)
                    {
                        // Can't do this on the last line
                        if (cli == lines.Count - 1) continue;

                        // Append the next line to this one
                        lines[cli].AddRange(lines[cli + 1]);

                        // Delete the next line
                        lines.RemoveAt(cli + 1);

                        /*** Update the screen ***/

                        // Remember the initial position
                        // Note: position stays the same
                        int c_left = Console.CursorLeft;
                        int c_top = Console.CursorTop;

                        // There is too much text to wipe, so the best move is to clear everything and rewrite
                        int[] start_rewriting_from = new int[2] { Console.CursorLeft, Console.CursorTop }; // Rewrite the current line as well (top - 1)
                        
                        for (int i = 0; i < lines.Count - cli + 1; i++)
                        {
                            Console.SetCursorPosition(0, Console.CursorTop + 1);
                            Console.Write(new string(' ', Console.BufferWidth));
                        }

                        Console.SetCursorPosition(start_rewriting_from[0], start_rewriting_from[1]);

                        Console.CursorLeft = 0;
                        Console.Write(GetNoteContent(cli));
                        Console.SetCursorPosition(c_left, c_top);
                    }
                    // If index is not at the beginning
                    else
                    {
                        // .RemoveAt(ci); causes NULL characters to be left 
                        // To circumvent this, a new List will be made without the added NULL character (placeholder)
                        // The main list will be cleared then refilled by the placeholder
                        lines[cli].RemoveAt(ci);
                        
                        // I was thinking this should be .Count - 1 but for some reason that messed everything up and .Count alone does the trick
                        char[] placeholder = new char[lines[cli].Count];
                        for (int i = 0; i < lines[cli].Count; i++)
                        {
                            placeholder[i] = lines[cli][i];
                        }

                        // Reset internal size of list (remove NULL char)
                        lines[cli].Clear();
                        lines[cli].AddRange(placeholder);

                        // Rewrite without char
                        int previous_cursor_pos = Console.CursorLeft;
                        Console.CursorLeft = 0;
                        Console.Write(new string(lines[cli].ToArray()) + " ");
                        Console.CursorLeft = previous_cursor_pos; // Cursor doesn't move
                    }
                }

                continue;
            }
            // Add closing markup tag [/] - Ctrl+/
            else if (keyinfo.Key == ConsoleKey.Oem2 && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                /***
                 * IMPORTANT: If the line has too many characters in it, overflow
                 * will be prevented by blocking new characters from being written
                 ***/
                if (lines[cli].Count + 3 >= Console.BufferWidth - 2) continue;

                // Insert the closing tag
                lines[cli].InsertRange(ci, "[/]");

                // Rewrite line
                int previous_loc = ci;
                Console.Write(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray());

                // Move cursor
                Console.CursorLeft = previous_loc + 3;
                ci = previous_loc + 3;

                continue;
            }
            // Add ending bracket when pressing the opening bracket [ => []
            else if (keyinfo.Key == ConsoleKey.Oem4)
            {
                /***
                 * IMPORTANT: If the line has too many characters in it, overflow
                 * will be prevented by blocking new characters from being written
                 ***/
                if (lines[cli].Count + 2 >= Console.BufferWidth - 2) continue;

                // Insert the two brackets
                lines[cli].InsertRange(ci, "[]");

                // Rewrite line
                int previous_loc = ci;
                Console.Write(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray());

                // Move cursor to inside the brackets
                Console.CursorLeft = previous_loc + 1;
                ci = previous_loc + 1;

                continue;
            }
            /***
             * Markup keybinds
             ***/
            else if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                /***
                 * Pressing one of these keybinds will
                 * insert a spectre.console markup style
                 * such as italics, underline, etc.
                 * 
                 * For colors, the user can press a number
                 * between 0 - 9, which he can map to a specific hex
                 * on his own by going to the settings via Alt+S
                 ***/
                List<char> _chars_to_add = new List<char>();

                switch (keyinfo.Key)
                {
                    // Italics - Ctrl+I
                    case ConsoleKey.I:
                        _chars_to_add.AddRange("[italic][/]");
                        break;
                    // Bold - Ctrl+B
                    // Blink - Ctrl+Shift+B
                    case ConsoleKey.B:
                        // Ctrl+B
                        if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) _chars_to_add.AddRange("[rapidblink][/]");
                        // Ctrl+Shift+B
                        else _chars_to_add.AddRange("[bold][/]");
                        break;
                    // Underline - Ctrl+U
                    case ConsoleKey.U:
                        _chars_to_add.AddRange("[underline][/]");
                        break;
                    // Strikethrough - Ctrl+Shift+5
                    case ConsoleKey.D5:
                        if (!keyinfo.Modifiers.HasFlag(ConsoleModifiers.Shift)) continue;
                        _chars_to_add.AddRange("[strikethrough][/]");
                        break;
                    // Dim - Ctrl+D
                    case ConsoleKey.D:
                        _chars_to_add.AddRange("[dim][/]");
                        break;
                    // No special keybind preseed
                    default: continue;
                }

                /***
                 * IMPORTANT: If the line has too many characters in it, overflow
                 * will be prevented by blocking new characters from being written
                 ***/
                if (lines[cli].Count + _chars_to_add.Count >= Console.BufferWidth - 2) continue;

                // Insert markup
                lines[cli].InsertRange(ci, _chars_to_add);

                // Get ci & cursor pos for after rewrite
                // -3 is for moving the cursor in between the markup tags (len [/])
                int loc = ci + _chars_to_add.Count - 3;

                // Rewrite line on screen
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.BufferWidth));
                Console.CursorLeft = 0;
                Console.Write(lines[cli].ToArray());

                Console.CursorLeft = loc;
                ci = loc;
                continue;
            }
            // Skip if the pressed key will **NOT** output an alphanumeric character
            else if (
                 ((int)keyinfo.Key < 48 || (int)keyinfo.Key > 111 || keyinfo.Key == ConsoleKey.LeftWindows || keyinfo.Key == ConsoleKey.RightWindows || keyinfo.Key == ConsoleKey.Applications || keyinfo.Key == ConsoleKey.Sleep)
                 && keyinfo.Key != ConsoleKey.Spacebar && keyinfo.Key != ConsoleKey.Tab
                 /* OEM keys */
                 && (int)keyinfo.Key < 186 && (int)keyinfo.Key > 223
            )
            {
                // If the key is not alphanum, don't show it
                continue;
            }

            // Check if the key event was 'Alt+V', the keybind for returning to the 'view' menu
            if (keyinfo.Key == ConsoleKey.V && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                // Switch to view mode
                UpdateMode(Mode.ViewNotes);
                KeyEventListenerPaused = false;
                return;
            }

            // Check if the key was TAB and if the cursor is within a set of markup tags [italic]cursor here[/]
            // If the above are true, move the cursor to just outside the closing tag [/]
            if (keyinfo.Key == ConsoleKey.Tab)
            {
                // This is done by finding both an opening and ending tag to the right of the 'ci' if one exists
                // If the [/] (closing tag) is closer than the opening tag, the 'ci' must be inside the markup
                string search_range = new string(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray())!;

                if (!search_range.Contains("[/]")) goto after_tab_markup_check;
                bool jump_to_tag_end = true;

                // Check if the search_range also contains an opening tag
                //                             (any)[anything but /](any)
                if (Regex.IsMatch(search_range, @"^(.*?)\[([^\/]*?)\](.*)$"))
                {
                    string opening_tag_content = Regex.Match(search_range, @"\[([^\/]*?)\]").Value;
                    int opening_tag_index = search_range.IndexOf(opening_tag_content);
                    int closing_tag_index = search_range.IndexOf("[/]");

                    // If the closing tag is closer than the opening tag, the cursor is inside the markup
                    jump_to_tag_end = closing_tag_index < opening_tag_index;
                }

                if (jump_to_tag_end)
                {
                    // Move the cursor to just outside the closing tag
                    ci += search_range.IndexOf("[/]") + 3;
                    Console.CursorLeft = ci;
                    continue;
                }
            }

            after_tab_markup_check:

            /***
             * The easiest way to fix the tab issue
             * where it appears as 8 spaces instead of a single \t char
             * is to treat the tab as 8 spaces
             * which is done by replacing it with 8 spaces
             * before it's saved to the note
             ***/

            /***
             * Below is where the pressed char is added to the note
             ***/

            // This array will typically hold only one key, but if tab is pressed, it will hold four spaces
            List<char> chars_to_add = new List<char>() { keyinfo.KeyChar };

            /***
             * IMPORTANT: If the line has too many characters in it, overflow
             * will be prevented by blocking new characters from being written
             ***/
            if (keyinfo.Key == ConsoleKey.Tab && lines[cli].Count + 4 >= Console.BufferWidth - 2) continue;
            else if (lines[cli].Count == Console.BufferWidth - 2) continue;

            // Pressing TAB will add four spaces instead of a \t char
            if (keyinfo.Key == ConsoleKey.Tab)
            {
                chars_to_add = new string(' ', Program.TAB_SIZE).ToList();
            }

            // If added to the end of the line
            if (ci == lines[cli].Count)
            {
                // Add the char(s) to the note
                lines[cli].AddRange(chars_to_add);
                Console.Write(chars_to_add.ToArray());
                ci += chars_to_add.Count;
            }
            else
            {
                /*** Insert char(s) into the note ***/
                lines[cli].InsertRange(ci, chars_to_add);
                ci += chars_to_add.Count;

                int previous_cursor_pos = Console.CursorLeft;
                Console.CursorLeft = 0;
                Console.Write(lines[cli].ToArray());
                Console.CursorLeft = previous_cursor_pos + chars_to_add.Count;
            }
        }

        // Create the note
        Notes.Add(new Note(
            title,
            Note.ParseLinksMarkup(GetNoteContent(0))
        ));
        SaveNotes();

        // Move range to very end to account for new note
        displayRange = new NotesRange((Notes.Count - (displayRangeCount + 1) > 0) ? Notes.Count - (displayRangeCount + 1) : 0, Notes.Count - 1);

        // Switch to view the new note
        UpdateMode(Mode.ViewNotes);
        KeyEventListenerPaused = false;
    }

    public static void DeleteNote(Note note)
    {
        Notes.RemoveAt(Notes.IndexOf(note));
        SaveNotes();
    }

    public static void EditNote(Note note, Note newNote)
    {
        Notes[Notes.IndexOf(note)] = newNote;
        SaveNotes();
    }
}