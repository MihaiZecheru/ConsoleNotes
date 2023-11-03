using Spectre.Console.Json;
using Spectre.Console;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ConsoleNotes;

struct TextRange
{
    /// <summary>
    /// The line the text range starts on, (the index, starts at 0)
    /// </summary>
    public int StartLineNumber { get; set; }

    /// <summary>
    /// The position of the starting character in <see cref="StartLineNumber"/>, (the index, starts at 0)
    /// </summary>
    public int StartCharacterPosition { get; set; }
    
    /// <summary>
    /// The line the text range ends on (the index, starts at 0)
    /// </summary>
    public int EndLineNumber { get; set; }

    /// <summary>
    /// The position of the ending character in <see cref="EndLineNumber"/>, (the index, starts at 0)
    /// </summary>
    public int EndCharacterPosition { get; set; }

    /// <summary>
    /// Is the selected text from left to right? Starting somewhere and moving to the right
    /// </summary>
    public bool IsLeftToRight { get; set; }

    public TextRange(int start_line_number, int start_character_position, int end_line_number, int end_character_position, bool is_left_to_right)
    {
        this.StartLineNumber = start_line_number;
        this.StartCharacterPosition = start_character_position;
        this.EndLineNumber = end_line_number;
        this.EndCharacterPosition = end_character_position;
        this.IsLeftToRight = is_left_to_right;
    }

    /// <summary>
    /// Check if no text is selected
    /// </summary>
    /// <returns>True if no text is selected</returns>
    public bool IsEmpty()
    {
        return StartLineNumber == -1 && EndLineNumber == -1 && StartCharacterPosition == -1 && EndCharacterPosition == -1;
    }
}

/// <summary>
/// Manages creating a new note or editing an existing one; a built-in console text editor
/// </summary>
internal class Editor
{
    /// <summary>
    /// The size of a tab in spaces
    /// </summary>
    private static int TAB_SIZE = 4;

    /// <summary>
    /// Save the current state every 4 characters
    /// </summary>
    private static int save_every = 4;

    /// <summary>
    /// The amount of chars that have been pressed since the last time the state was saved
    /// <br/><br/>
    /// Used to know when to save the current state
    /// </summary>
    private int chars_pressed = 0;

    /// <summary>
    /// Current Line Index in the note
    /// </summary>
    private int cli = 0;

    /// <summary>
    /// Current Character Index in the current line of the note
    /// </summary>
    private int ci = 0;

    /// <summary>
    /// The lines that make up the note
    /// <br/><br/>
    /// Access the current character with lines[cli][ci]
    /// </summary>
    private List<List<char>> lines = new() { new List<char>() };

    /// <summary>
    /// Keep track of changes made to the note
    /// </summary>
    private CtrlZ states = new CtrlZ();

    /// <summary>
    /// If the note is JSON-Only
    /// </summary>
    private bool isJson = false;

    /// <summary>
    /// If an existing note is being edited, this will be false
    /// <br/><br/>
    /// If a new note is being created, this will be true
    /// </summary>
    private bool editing_existing_note = false;

    /// <summary>
    /// Keep track of the selected/highlighted text
    /// </summary>
    private TextRange selected_text_range = new(-1, -1, -1, -1, false);

    /// <summary>
    /// Concatenate the note into a single string
    /// </summary>
    /// <param name="start">The index of the line to start concatenating from</param>
    /// <param name="trim">If the note should be trimmed</param>
    /// <returns>The concatenated note which includes all lines from [start, lines.Count - 1]</returns>
    private string GetNoteContent(int start, bool trim = false)
    {
        string s = string.Empty;

        for (int i = start; i < lines.Count; i++)
        {
            for (int j = 0; j < lines[i].Count; j++) s += lines[i][j];
            if (i != lines.Count - 1) s += '\n';
        }

        return trim ? s.Trim() : s;
    }

    /// <summary>
    /// Get the currently selected text based on the selected_text_range field
    /// </summary>
    /// <returns></returns>
    private string GetSelectedContent()
    {
        if (selected_text_range.IsEmpty()) return string.Empty;

        string s = string.Empty;
        for (int i = selected_text_range.StartLineNumber; i <= selected_text_range.EndLineNumber; i++)
        {
            if (lines.Count == i) break; // Will give IndexOutOfRangeException when lines[i] is accessed if no check

            // Empty line
            if (lines[i].Count == 0)
            {
                s += '\n';
                continue;
            }
            
            for (int j = 0; j < lines[i].Count; j++)
            {
                // Skip characters before the start of the selected text if on the starting line
                if (i == selected_text_range.StartLineNumber && j < selected_text_range.StartCharacterPosition) continue;
                // Skip characters after the end of the selected text if on the ending line
                if (i == selected_text_range.EndLineNumber && j > selected_text_range.EndCharacterPosition) break;
                // Add char
                s += lines[i][j];
            }

            if (i != selected_text_range.EndLineNumber) s += '\n';
        }

        return s;
    }

    // *** //

    /// <summary>
    /// Create the Editor object
    /// <br/><br/>
    /// Call editor.Mainloop() to activate the editor
    /// </summary>
    public Editor()
    {
        // Prevent Ctrl+C from closing the application as users might accidentally quit while copying a note's content
        Console.TreatControlCAsInput = true;

        // Default colors
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
    }

    /// <summary>
    /// Open the Editor to edit an existing note
    /// <br/><br/>
    /// Call editor.Mainloop() to activate the editor
    /// </summary>
    /// <param name="existing_note">The note to open the Editor to</param>
    public Editor(Note existing_note)
    {
        this.editing_existing_note = true;
        this.isJson = existing_note.IsJson;

        // Prevent Ctrl+C from closing the application as users might accidentally quit while copying a note's content
        Console.TreatControlCAsInput = true;

        // Default colors
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;

        // Split the note into lines
        string[] lines = existing_note.Body.Split('\n');

        // Add each line to the note
        this.lines.Clear();
        for (int i = 0; i < lines.Length; i++)
        {
            this.lines.Add(lines[i].ToList());
        }

        // Set to last line
        this.cli = this.lines.Count - 1;
        
        // Set to last character in last line
        this.ci = this.lines[this.cli].Count;

        // Save the current state as the first state
        this.states.AddState(this.lines, Tuple.Create(ci, cli));
        this.states.RemoveAt(0);

        // Print the note
        Console.Write(existing_note.Body);
    }

    /// <summary>
    /// Start the editor in the console and allow the user to edit / make their note
    /// </summary>
    /// <param name="note_body">Reference to a variable, that, when MainLoop finishes, will hold the content of the note the user has written</param>
    /// <param name="_isJson">Reference to a variable, that, when MainLoop finishes, will hold a value indicating whether or not the note is JSON-Only</param>
    /// <returns>A success value indicating whether or not the user finished writing their note</returns>
    public bool MainLoop(ref string note_body, ref bool _isJson)
    {
        // If the program fails at any point, save the note and the error message in a crash log
        // No indent to make it easier to read
        try {
        // Loop for getting input
        while (true)
        {
            ConsoleKeyInfo keyinfo = Console.ReadKey(true);
            bool ctrl = keyinfo.Modifiers.HasFlag(ConsoleModifiers.Control);
            bool shift = keyinfo.Modifiers.HasFlag(ConsoleModifiers.Shift);

            // Create then save the note - Ctrl+S 
            if (keyinfo.Key == ConsoleKey.S && ctrl)
            {
                if (this.CtrlS())
                {
                    note_body = Note.ParseLinksMarkup(GetNoteContent(0, true));
                    _isJson = this.isJson;
                    return true;
                }

                continue;
            }
            // Toggle JSON-Only
            else if (keyinfo.Key == ConsoleKey.J && ctrl)
            {
                this.CtrlJ();
                continue;
            }
            // Undo - Ctrl+Z
            else if (keyinfo.Key == ConsoleKey.Z && ctrl)
            {
                if (select_mode) this.DeselectText();
                this.CtrlZ();
                continue;
            }
            // Redo - Ctrl+Y
            else if (keyinfo.Key == ConsoleKey.Y && ctrl)
            {
                if (select_mode) this.DeselectText();
                this.CtrlY();
                continue;
            }
            // Create new line - Enter
            else if (keyinfo.Key == ConsoleKey.Enter)
            {
                // Replace with newline (Enter) if text is selected
                if (select_mode) this.DeleteSelectedText();
                // Runs regardless
                this.Enter();
                continue;
            }
            // Move to beginning of line - Home
            else if (keyinfo.Key == ConsoleKey.Home)
            {
                if (select_mode) this.DeselectText();
                this.Home();
                continue;
            }
            // Move to end of line - End
            else if (keyinfo.Key == ConsoleKey.End)
            {
                if (select_mode) this.DeselectText();
                this.End();
                continue;
            }
            // Move to beginning of text - Ctrl+H & Ctrl+UpArrow
            // Ctrl+Home doesn't register as an event, so Ctrl+H is used instead
            else if ((keyinfo.Key == ConsoleKey.H || keyinfo.Key == ConsoleKey.UpArrow) && ctrl)
            {
                if (select_mode) this.DeselectText();
                this.CtrlH();
                continue;
            }
            // Ctrl+End doesn't register as an event, so Ctrl+E is used instead
            // Move to end of text - Ctrl+E & Ctrl+DownArrow
            else if ((keyinfo.Key == ConsoleKey.E || keyinfo.Key == ConsoleKey.DownArrow) && ctrl)
            {
                if (select_mode) this.DeselectText();
                this.CtrlE();
                continue;
            }
            // TODO: for each of these shift+direction functions, do something to the selected_text_range
            // Highlight text left one word - Ctrl+Shift+LeftArrow
            else if (keyinfo.Key == ConsoleKey.LeftArrow && ctrl && shift)
            {
                this.CtrlShiftLeftArrow();
                continue;
            }
            // Highlight text right one word - Ctrl+Shift+RightArrow
            else if (keyinfo.Key == ConsoleKey.RightArrow && ctrl && shift)
            {
                this.CtrlShiftRightArrow();
                continue;
            }
            // Highlight text left one character - Shift+LeftArrow
            else if (keyinfo.Key == ConsoleKey.LeftArrow && shift)
            {
                this.ShiftLeftArrow();
                continue;
            }
            // Highlight text right one character - Shift+RightArrow
            else if (keyinfo.Key == ConsoleKey.RightArrow && shift)
            {
                this.ShiftRightArrow();
                continue;
            }
            //// Highlight text to beginning of paragraph - Ctrl+Shift+Up
            //else if (keyinfo.Key == ConsoleKey.UpArrow && ctrl && shift)
            //{
            //    this.CtrlShiftUp();
            //    continue;
            //}
            //// Highlight text to end of paragraph - Ctrl+Shift+Down
            //else if (keyinfo.Key == ConsoleKey.DownArrow && ctrl && shift)
            //{
            //    this.CtrlShiftDown();
            //    continue;
            //}
            //// Highlight text up one line - Shift+UpArrow
            //else if (keyinfo.Key == ConsoleKey.UpArrow && shift)
            //{
            //    this.ShiftUpArrow();
            //    continue;
            //}
            //// Highlight text down one line - Shift+DownArrow
            //else if (keyinfo.Key == ConsoleKey.DownArrow && shift)
            //{
            //    this.ShiftDownArrow();
            //    continue;
            //}
            // Highlight all text - Ctrl+A
            else if (keyinfo.Key == ConsoleKey.A && ctrl)
            {
                this.CtrlA();
                continue;
            }
            // Copy selected text - Ctrl+C
            else if (keyinfo.Key == ConsoleKey.C && ctrl)
            {
                // Note: Console.TreatControlCAsInput = true in Program.cs and on class initialization
                this.CtrlC();
                this.DeselectText();
                continue;
            }
            // Move cursor left one word - Ctrl+LeftArrow
            else if (keyinfo.Key == ConsoleKey.LeftArrow && ctrl)
            {
                if (select_mode) this.DeselectText();
                this.CtrlLeftArrow();
                continue;
            }
            // Move cursor left one character - LeftArrow
            else if (keyinfo.Key == ConsoleKey.LeftArrow)
            {
                if (select_mode) this.DeselectText();
                this.LeftArrow();
                continue;
            }
            // Move cursor right one word - Ctrl+RightArrow
            else if (keyinfo.Key == ConsoleKey.RightArrow && ctrl)
            {
                if (select_mode) this.DeselectText();
                this.CtrlRightArrow();
                continue;
            }
            // Move cursor right one character - RightArrow
            else if (keyinfo.Key == ConsoleKey.RightArrow)
            {
                if (select_mode) this.DeselectText();
                this.RightArrow();
                continue;
            }
            // Move cursor down once - DownArrow
            else if (keyinfo.Key == ConsoleKey.DownArrow)
            {
                if (select_mode) this.DeselectText();
                this.DownArrow();
                continue;
            }
            // Move cursor up once - UpArrow
            else if (keyinfo.Key == ConsoleKey.UpArrow)
            {
                if (select_mode) this.DeselectText();
                this.UpArrow();
                continue;
            }
            // Delete previous word - Ctrl+Backspace
            else if (keyinfo.Key == ConsoleKey.Backspace && ctrl)
            {
                if (select_mode) this.DeleteSelectedText();
                else this.CtrlBackspace();
                continue;
            }
            // Delete previous character - Backspace
            else if (keyinfo.Key == ConsoleKey.Backspace)
            {
                if (select_mode) this.DeleteSelectedText();
                else this.Backspace();
                continue;
            }
            // Delete next word - Ctrl+Delete
            else if (keyinfo.Key == ConsoleKey.Delete && ctrl)
            {
                if (select_mode) this.DeleteSelectedText();
                else this.CtrlDelete();
                continue;
            }
            // Delete next character - Delete
            else if (keyinfo.Key == ConsoleKey.Delete)
            {
                if (select_mode) this.DeleteSelectedText();
                else this.Delete();
                continue;
            }
            // Delete current line - Ctrl+K
            else if (keyinfo.Key == ConsoleKey.K && ctrl)
            {
                if (select_mode) this.DeleteSelectedText();
                else this.CtrlShiftK();
                continue;
            }
            // Add closing markup tag [/] - Ctrl+/
            else if (keyinfo.Key == ConsoleKey.Oem2 && ctrl)
            {
                // Replace with closing tag if text is selected
                if (select_mode) this.DeleteSelectedText();
                // Runs regardless
                this.CtrlForwardSlash();
                continue;
            }
            // Add ending bracket when pressing the opening bracket [ => []
            else if (keyinfo.Key == ConsoleKey.Oem4 && !shift)
            {
                // TODO: this is because i assume people will paste json into the editor, which would cause the brackets to become messed up. todo is find a way to detect when text is being pasted to fix this issue
                // Don't do this while in JSON-Only mode
                if (!isJson)
                {
                    // Replace with closing bracket if text is selected
                    if (select_mode) this.DeleteSelectedText();
                    // Runs regardless
                    this.ClosingBracket(0);
                    continue;
                }
            }
            // Add ending bracket when pressing the opening bracket ( => ()
            else if (keyinfo.Key == ConsoleKey.D9 && shift)
            {
                if (!isJson)
                {
                    // Replace with closing bracket if text is selected
                    if (select_mode) this.DeleteSelectedText();
                    // Runs regardless
                    this.ClosingBracket(1);
                    continue;
                }
            }
            // Add ending bracket when pressing the opening bracket { => {}
            else if (keyinfo.Key == ConsoleKey.Oem4 && shift)
            {
                if (!isJson)
                {
                    // Replace with closing bracket if text is selected
                    if (select_mode) this.DeleteSelectedText();
                    // Runs regardless
                    this.ClosingBracket(2);
                    continue;
                }
            }
            // Ctrl+Q: quit without saving
            else if (keyinfo.Key == ConsoleKey.Q && ctrl)
            {
                return false;
            }
            /***
             * Markup keybinds
             ***/
            else if (ctrl)
            {
                // Don't do this while in JSON-Only mode
                if (!isJson)
                { // TODO: add something for selected text here
                    this.HandleMarkupKeybinds(keyinfo);
                    continue;
                }
            }
            // Skip if the pressed key will **NOT** output an alphanumeric character
            else if (
                 // This check for LeftWindows etc. is because these few obscure keys are inbetween the range of 48 - 111, so they are not excluded by the first two checks of < 48 & > 111
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
            if (keyinfo.Key == ConsoleKey.V && keyinfo.Modifiers.HasFlag(ConsoleModifiers.Alt)) return false;

            /*** Done with main checks for shortcuts ***/

            /***
             * Below is where the char that is a result of a regular keypress is added to the note
             ***/

            // Selected text replaces selected text when a character is pressed while text is selected
            if (select_mode)
            {
                this.DeleteSelectedText();
            }

            // Check if the key was TAB and if the cursor is within a set of markup tags [italic]cursor here[/]
            // If the above are true, move the cursor to just outside the closing tag [/]
            if (keyinfo.Key == ConsoleKey.Tab)
            {
                if (this.Tab()) continue;
            }

            /***
             * Because pressing [ will automatically add a closing bracket,
             * If a user presses the closing bracket themselves,
             * another bracket shouldn't be added
             * 
             * Instead the cursor will move one space to the right
             * to simulate having added the bracket, despite it already being there
             * This is something IDEs and text editors often implement
             * 
             * This if statements checks the same thing for regular parenthesis ()
             * 
             * The check in the middle is to prevent an IndexOutOfRange exception
             ***/
            if (
                ci < lines[cli].Count
                &&
                (
                    (keyinfo.Key == ConsoleKey.Oem6 && lines[cli][ci] == ']' && !shift)
                    ||
                    (keyinfo.Key == ConsoleKey.Oem6 && lines[cli][ci] == '}' && shift)
                    ||
                    (keyinfo.Key == ConsoleKey.D0 && shift && lines[cli][ci] == ')')
                )
            )
            {
                ci++;
                Console.CursorLeft++;
                continue;
            }

            /***
             * The easiest way to fix the tab issue
             * where it appears as 8 spaces instead of a single \t char
             * is to treat the tab as <TAB_SIZE> spaces
             * which is done by replacing it with <TAB_SIZE> spaces
             * before it's saved to the note
             * 
             * This is done a a few lines further down
             ***/

            // This array will typically hold only one key, but if tab is pressed, it will hold four spaces
            List<char> chars_to_add = new List<char>() { keyinfo.KeyChar };

            /***
             * IMPORTANT: If the line has too many characters in it, overflow
             * will be prevented by entering a new line automatically and continuing
             * on the next line
             ***/
            // The tab key enters multiple spaces so a separate check is needed
            if ((keyinfo.Key == ConsoleKey.Tab && lines[cli].Count + 4 >= Console.BufferWidth - 2) || lines[cli].Count == Console.BufferWidth - 2)
            {
                /***
                 * This code will trigger when the current line is full.
                 * Text will automatically be moved to the next line, which
                 * is done by creating a new line.
                 * 
                 * Automatically moving to the next line is useless if it's
                 * done halfway through a word, though, so the entire word
                 * needs to be moved.
                 * 
                 * This is done by moving the cursor/ci to the last space
                 * and pressing enter there, which will move the entire word to the next line
                 ***/
                
                ci = lines[cli].LastIndexOf(' ') + 1;
                int chars_moved = lines[cli].Count - ci;
                Console.CursorLeft = ci;
                this.Enter();
                ci += chars_moved;
                Console.CursorLeft = ci;
            }

            // Pressing TAB will add four spaces instead of a \t char
            if (keyinfo.Key == ConsoleKey.Tab)
            {
                chars_to_add = new string(' ', TAB_SIZE).ToList();
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

            // Check save
            chars_pressed++; // TAB will only count as one change despite adding <TAB_SIZE> spaces
            if (chars_pressed >= save_every)
            {
                states.AddState(lines, Tuple.Create(ci, cli));
                chars_pressed = 0;
            }
        }
        }
        catch (Exception e)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string crashLogPath = Path.Combine(System.IO.Directory.GetCurrentDirectory(), "crash_log", timestamp);

            // Create the crash log folder
            Directory.CreateDirectory(crashLogPath);

            // Save note
            string _note = GetNoteContent(0);
            File.WriteAllText(Path.Combine(crashLogPath, "note.txt"), _note);
            TextCopy.ClipboardService.SetText(_note);

            // Save error message
            File.WriteAllText(Path.Combine(crashLogPath, "error.txt"), e.ToString());

            // Show error message
            string note_path = Path.Combine(crashLogPath, "note.txt");
            Program.MessageBox((IntPtr)0, $"The application crashed due to an unknown error. Your note has been copied to clipboard and saved to\n\n{note_path}", "Application Crashed", 0);

            try
            {
                // Open file explorer to the crash log
                System.Diagnostics.Process.Start(crashLogPath);
            } catch (Exception)
            {
                // If there is an access denied error, due to the application being run from inside C:/Users/<user>/, ask to copy (yes or no buttons)
                int result = Program.MessageBox((IntPtr)0, "Error opening crash log in file explorer - Access to file denied. Do you want to copy the file path to clipboard?", "Error opening crash log", 4);

                // If user presses 'Yes'
                if (result == 6)
                {
                    TextCopy.ClipboardService.SetText(crashLogPath);
                }
            }

            // Close the editor
            return false;
        }
    }

    // *** Editor Keypresses & Shortcuts *** //

    /// <summary>
    /// Ctrl+S - Save Note
    /// </summary>
    /// <returns>Success value indicating whether or not the note was compiled and saved</returns>
    private bool CtrlS()
    {
        // note len must be > 0
        if (GetNoteContent(0).Length <= 0) return false;

        /***
            * As users are allowed to write markup using [style]content[/] syntax,
            * there is a chance for user error to leave unmatched brackets, or to use
            * a square bracket and forgetting to escape it when using the bracket normally.
            * 
            * An attempt will be made to compile the note prematurely, and if it fails,
            * the user will get a popup letting them know the note couldn't compile.
            ***/
        string note = GetNoteContent(0, true);
        try
        {
            /*** Attempt to compile ***/
            if (isJson)
            {
                JsonText json = new Spectre.Console.Json.JsonText(note);

                // The .Build method is called when trying to print to the console,
                // but the input should be captured so that it doesn't actually write anything
                // to the console, and instead just calls the .Build method internally.
                // If there is an error, it will be caught and the error message will be shown,
                // with the text on the console remaining unchanged.

                // Temporarily capture the console input
                bool stop = false;
                new Thread(() =>
                {
                    while (true)
                    {
                        if (stop) break;
                        Console.ReadKey(true);
                    }
                }).Start();

                // Invoke the .Build method
                AnsiConsole.Write(json);

                // Stop capturing console input
                stop = true;
            }
            else
            {
                // Compile non-json note
                new Markup(note);

                // If the compilation doesn't work an error will be thrown
            }

            // return statement wont be reached if there is an error with compiling the Json/markup
            return true; // Note will be created shortly after
        }
        catch (InvalidOperationException e)
        {
            if (isJson)
            {
                int result = Program.MessageBox((IntPtr)0, $"Check your JSON for any errors. There may be a missing bracket or comma. " +
                    $"\n\nError: {e.Message}" +
                    $"\n\nDo you want to open a JSON validator in your browser? Your note will be copied to clipboard.",
                "JSON Syntax Error", 4);

                // If user presses 'Yes'
                if (result == 6)
                {
                    // Go to json validator website
                    Process.Start(new ProcessStartInfo("cmd", $"/c start https://jsonlint.com/") { CreateNoWindow = true });

                    // Copy currently written text to clipboard for quickly pasting it in to check what's wrong
                    TextCopy.ClipboardService.SetText(note);
                }
            }
            else
            {
                Program.MessageBox((IntPtr)0, $"Check your note for any markup errors. Make sure all square brackets are escaped with [[ or ]].\n\nError: {e.Message}", "Markup Syntax Error", 0);
            }

            return false;
        }
    }

    /// <summary>
    /// Ctrl+J - Toggle JSON-Only by toggling the state of <see cref="isJson"/>
    /// </summary>
    private void CtrlJ()
    {
        // The header_text is the text displayed in the line rule at the top of the screen
        string header_text;

        if (isJson)
        {
            // Disable JSON-Only
            isJson = false;
            header_text = this.editing_existing_note ? "Edit Note" : "Write A New Note";
        }
        else
        {
            // Enable JSON-Only
            isJson = true;
            header_text = this.editing_existing_note ? "Edit Note - JSON Only" : "Write A New Note - JSON Only";
        }

        // Remember cursor position
        int cursor_pos_left = Console.CursorLeft;
        int cursor_pos_top = Console.CursorTop;

        // Make the rule
        var rule = new Spectre.Console.Rule($"[deeppink3]{header_text}[/]");
        rule.Style = new Style(Color.Yellow);

        // Replace rule on screen
        Console.SetCursorPosition(0, 0);
        AnsiConsole.Write(rule);
        Console.SetCursorPosition(cursor_pos_left, cursor_pos_top);
    }

    /// <summary>
    /// Ctrl+Z - Undo
    /// <br/><br/>
    /// Revert to a previous state
    /// </summary>
    private void CtrlZ()
    {
        // Get previous state
        (lines, (ci, cli)) = states.Undo();

        /*** Rewrite screen ***/

        // Clear - cursor will automatically be brought to (0, 0)
        Console.Clear();

        // Rewrite the horizontal rule
        string rule_header;
        if (isJson) rule_header = this.editing_existing_note ? "Edit Note - JSON Only" : "Write A New Note - JSON Only";
        else rule_header = this.editing_existing_note ? "Edit Note" : "Write A New Note";

        var rule = new Spectre.Console.Rule($"[deeppink3]{rule_header}[/]");
        rule.Style = new Style(Color.Yellow);
        AnsiConsole.Write(rule);

        // Rewrite the note
        for (int i = 0; i < lines.Count; i++)
        {
            Console.WriteLine(lines[i].ToArray());
        }

        // Move cursor to the correct position
        Console.SetCursorPosition(ci, cli + 1); // Account for horizontal rule with top + 1
    }

    /// <summary>
    /// Ctrl+Y - Redo
    /// <br/><br/>
    /// Revert to a more recent state
    /// </summary>
    private void CtrlY()
    {
        // Get following state
        (lines, (ci, cli)) = states.Redo();

        /*** Rewrite screen ***/

        // Clear - cursor will automatically be brought to (0, 0)
        Console.Clear();

        // Rewrite the horizontal rule
        string rule_header;
        if (isJson) rule_header = this.editing_existing_note ? "Edit Note - JSON Only" : "Write A New Note - JSON Only";
        else rule_header = this.editing_existing_note ? "Edit Note" : "Write A New Note";

        var rule = new Spectre.Console.Rule($"[deeppink3]{rule_header}[/]");
        rule.Style = new Style(Color.Yellow);
        AnsiConsole.Write(rule);

        // Rewrite the note
        for (int i = 0; i < lines.Count; i++)
        {
            Console.WriteLine(lines[i].ToArray());
        }

        // Move cursor to the correct position
        Console.SetCursorPosition(ci, cli + 1); // Account for horizontal rule with top + 1
    }

    /// <summary>
    /// Enter key - New line
    /// </summary>
    private void Enter()
    {
        /*** Creates a new line ***/

        /***
         * IMPORTANT: if the cursor is at the bottom of the screen
         * do not allow any text to be written
         * 
         * The user must expand their console to write more
         ***/
        if (cli == Console.BufferHeight - 3) return;

        // From the current line, get text until end of line
        List<char> new_line = lines[cli].GetRange(ci, lines[cli].Count - ci);

        // Remove that text from current line
        lines[cli].RemoveRange(ci, lines[cli].Count - ci);

        // Add the new line
        lines.Insert(++cli, new_line);
        ci = 0;

        // Clear screen
        Console.SetCursorPosition(0, 1);
        for (int i = 0; i < lines.Count; i++)
        {
            Console.WriteLine(new string(' ', Console.BufferWidth));
        }

        // Rewrite note
        Console.SetCursorPosition(0, 1);
        for (int i = 0; i < lines.Count; i++)
        {
            Console.WriteLine(new string(lines[i].ToArray()));
        }

        // Move cursor to next line
        Console.SetCursorPosition(0, cli + 1); // +1 for header line

        // Check save
        chars_pressed += 2; // Enter counts as 2 chars pressed
        if (chars_pressed >= save_every)
        {
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
    }

    /// <summary>
    /// Home key - Move to the beginning of the current line
    /// </summary>
    private void Home()
    {
        ci = 0;
        Console.CursorLeft = 0;
    }

    /// <summary>
    /// End key - Move to the end of the current line
    /// </summary>
    private void End()
    {
        ci = lines[cli].Count;
        Console.CursorLeft = lines[cli].Count;
    }

    /// <summary>
    /// Ctrl+H & Ctrl+DownArrow - Move to the end of the note
    /// </summary>
    private void CtrlH()
    {
        ci = 0;
        cli = 0;
        Console.SetCursorPosition(0, 1);
    }

    /// <summary>
    /// Ctrl+E & Ctrl+UpArrow - Move to the beginning of the note
    /// </summary>
    private void CtrlE()
    {
        cli = lines.Count - 1;
        ci = lines[cli].Count;
        Console.SetCursorPosition(ci, cli + 1); // +1 for header line
    }

    /// <summary>
    /// Shift+LeftArrow - Toggle the selection of the character to the left
    /// </summary>
    private void ShiftLeftArrow()
    {
        // If the selection is from left to right, going left will shorten the selection (unselect)
        // Otherwise, if the selection is going from right to left, by going left the selection will be extended

        // Start a new selection if nothing is yet selected
        if (selected_text_range.IsEmpty())
        {
            // If the cursor cannot go any further left on the current line
            if (ci == 0)
            {
                // If the current line is the first line, do nothing
                if (cli == 0) return;
                cli--;
                
                ci = lines[cli].Count - 1;
                bool empty_line = ci == -1;
                if (empty_line) ci = 0;
                
                selected_text_range = new(cli, ci, cli, ci, false);
                EnableSelectMode();

                // Highlight the last character of the previous line
                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                if (empty_line) Console.Write(' ');
                else Console.Write(lines[cli][ci]);
                
                Console.CursorLeft--;
                SetDefaultBGandFG();
                return;
            }

            // The start and end lines are the same (cli)
            // As the highlighting direction is left, the character that will be selected is the one to the left of the current char,
            // meaning ci - 1
            // Only one character is selected, so the start and end character positions are equal to each other

            // Is right to left, not left to right
            selected_text_range = new(cli, ci - 1, cli, ci - 1, false);
            Console.CursorLeft--;

            // Higlight the newly selected character
            SetSelectBGandFG();
            Console.Write(lines[cli][--ci]);
            // The cursor is moved back because it should be on the left side of what was highlighted,
            // and the Console.Write moved it to the right side
            Console.CursorLeft--;
            SetDefaultBGandFG();
            EnableSelectMode();
        }
        // Is Left to Right (towards end)
        // Shorten selection
        else if (selected_text_range.IsLeftToRight)
        {
            selected_text_range.EndCharacterPosition--;
            ci--;
            Console.CursorLeft--;

            // Rewrite newly unselected character without highlight
            SetDefaultBGandFG();
            Console.Write(lines[cli][ci]);
            Console.CursorLeft--;
            SetDefaultBGandFG();
        }
        // Is Right to Left (towards start)
        // Extend selection to the left
        else
        {
            // If the range cannot be extended any further left on the current line
            if (selected_text_range.StartCharacterPosition == 0)
            {
                // If the current line is the first line, do nothing
                if (cli == 0) return;
                cli--;

                // Highlight the last character of the previous line
                ci = lines[cli].Count - 1;
                bool empty_line = ci == -1;
                if (empty_line) ci = 0;

                selected_text_range.StartLineNumber--;
                selected_text_range.StartCharacterPosition = ci;

                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();
                
                // If empty line
                if (empty_line) Console.Write(' ');
                else Console.Write(lines[cli][ci]);

                Console.CursorLeft--;
                SetDefaultBGandFG();
                return;
            }

            // If the range CAN be extended further left on the current line

            // The range gets extended left (meaning towards a lesser index), so end position is decreased
            selected_text_range.StartCharacterPosition--;
            ci--;
            Console.CursorLeft--;

            // Rewrite newly selected character with highlight
            SetSelectBGandFG();
            Console.Write(lines[cli][ci]);
            // The cursor is moved back because it should be on the left side of what was highlighted,
            // and the Console.Write moved it to the right side
            Console.CursorLeft--;
            SetDefaultBGandFG();
        }
    }

    private void ShiftRightArrow()
    {
        // If the selection is from left to right, going right will lengthen the selection (select more)
        // Otherwise, if the selection is going from right to left, by going right the selection will be extended

        // Start a new selection if nothing is yet selected
        if (selected_text_range.IsEmpty())
        {
            // If the cursor cannot go any further right on the current line
            if (ci == lines[cli].Count)
            {
                // Check if the current line is the last line; if it's possible to create a selection on the next line
                if (cli == lines.Count - 1) return;

                cli++;
                ci = 0;
                selected_text_range = new(cli, ci, cli, ci, true);

                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                // If empty_line
                if (lines[cli].Count == 0) Console.Write(' ');
                else Console.Write(lines[cli][ci++]);

                SetDefaultBGandFG();
                EnableSelectMode();
                return;
            }

            // The start and end lines are the same (cli)
            // As the highlighting direction is right, the character that will be selected is the current char
            // Example: in "l^orem", (^ is cursor pos) going to the right will highlight the o, even though the cursor is at o
            // Only one character is selected (ci), so the start and end character positions are equal to each other

            // Is left to right, not right to left
            selected_text_range = new(cli, ci, cli, ci, true);

            // Higlight the newly selected character
            SetSelectBGandFG();
            Console.Write(lines[cli][ci++]);
            SetDefaultBGandFG();
            EnableSelectMode();
        }
        // Is Left to Right (towards end)
        // Lengthen selection
        else if (selected_text_range.IsLeftToRight)
        {
            // If the range cannot be extended any further right
            if (selected_text_range.EndCharacterPosition == lines[cli].Count - 1 || lines[cli].Count == 0)
            {
                // Check if the current line is the last line; if it's possible to extend the selection to the next line
                if (selected_text_range.EndLineNumber == lines.Count - 1) return;

                selected_text_range.EndLineNumber++;
                cli++;
                ci = 0;
                selected_text_range.EndCharacterPosition = 0;
                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                // If empty_line
                if (lines[cli].Count == 0) Console.Write(' ');
                else Console.Write(lines[cli][ci++]);

                SetDefaultBGandFG();
                return;
            }

            selected_text_range.EndCharacterPosition++;

            // Rewrite newly selected character with highlight
            SetSelectBGandFG();
            Console.Write(lines[cli][ci++]);
            SetDefaultBGandFG();
        }
        // Is Right to Left (towards start)
        // Shorten selection to the right
        else
        {
            // The range gets shortened right
            selected_text_range.StartCharacterPosition++;

            // Rewrite newly unselected character without highlight
            SetDefaultBGandFG();
            Console.Write(lines[cli][ci++]);
        }
    }

    private void CtrlShiftLeftArrow()
    {
        // If the selection is from left to right, going left will shorten the selection (unselect)
        // Otherwise, if the selection is going from right to left, by going left the selection will be extended

        int x = CharsLeftUntillNextSpaceOrEOL(cli ,ci);

        // Start a new selection if nothing is yet selected
        if (selected_text_range.IsEmpty())
        {
            // If the range cannot be extended any further left on the current line
            if (ci == 0)
            {
                // If the current line is the first line, do nothing
                if (cli == 0) return;
                cli--;

                ci = lines[cli].Count - 1;
                bool empty_line = ci == -1;
                if (empty_line) ci = 0;

                selected_text_range = new(cli, ci, cli, ci, false);

                // Highlight the last character of the previous line
                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                if (empty_line) Console.Write(' ');
                else Console.Write(lines[cli][ci]);

                Console.CursorLeft--;
                SetDefaultBGandFG();
                EnableSelectMode();
                return;
            }

            // Is right to left, not left to right
            selected_text_range = new(cli, ci - x, cli, ci, false);
            Console.CursorLeft -= x;

            // Higlight the newly selected character
            SetSelectBGandFG();
            Console.Write(lines[cli].GetRange(ci - x, x).ToArray());
            ci -= x;
            // The cursor is moved back because it should be on the left side of what was highlighted,
            // and the Console.Write moved it to the right side
            Console.CursorLeft -= x;
            SetDefaultBGandFG();
            EnableSelectMode();
        }
        // Is Left to Right (towards end)
        // Shorten selection
        else if (selected_text_range.IsLeftToRight)
        {
            selected_text_range.EndCharacterPosition -= x;
            Console.CursorLeft -= x;

            // Rewrite newly unselected character without highlight
            SetDefaultBGandFG();
            Console.Write(lines[cli].GetRange(ci - x, x).ToArray());
            ci -= x;
            Console.CursorLeft -= x;
            SetDefaultBGandFG();
        }
        // Is Right to Left (towards start)
        // Extend selection to the left
        else
        {
            // If the range cannot be extended any further left on the current line
            if (selected_text_range.StartCharacterPosition == 0)
            {
                // If the current line is the first line, do nothing
                if (cli == 0) return;
                cli--;

                ci = lines[cli].Count - 1;
                bool empty_line = ci == -1;
                if (empty_line) ci = 0;

                selected_text_range.StartLineNumber--;
                selected_text_range.StartCharacterPosition = ci;

                // Highlight the last character of the previous line
                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                if (empty_line) Console.Write(' ');
                else Console.Write(lines[cli][ci]);

                Console.CursorLeft--;
                SetDefaultBGandFG();
                return;
            }

            // If the range CAN be extended further left on the current line

            // The range gets extended left (meaning towards a lesser index), so end position is decreased
            selected_text_range.StartCharacterPosition -= x;
            Console.CursorLeft -= x;

            // Rewrite newly selected character with highlight
            SetSelectBGandFG();
            Console.Write(lines[cli].GetRange(ci - x, x).ToArray());
            ci -= x;
            // The cursor is moved back because it should be on the left side of what was highlighted,
            // and the Console.Write moved it to the right side
            Console.CursorLeft -= x;
            SetDefaultBGandFG();
        }
    }

    private void CtrlShiftRightArrow()
    {
        // If the selection is from left to right, going left will shorten the selection (unselect)
        // Otherwise, if the selection is going from right to left, by going left the selection will be extended

        int x = CharsRightUntilNextSpaceOrEOL(cli, ci);

        // Start a new selection if nothing is yet selected
        if (selected_text_range.IsEmpty())
        {
            // If the cursor cannot go any further right on the current line
            if (ci == lines[cli].Count)
            {
                // Check if the current line is the last line; if it's possible to create a selection on the next line
                if (cli == lines.Count - 1) return;

                cli++;
                ci = 0;
                selected_text_range = new(cli, ci, cli, ci, true);

                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                //  If empty_line
                if (lines[cli].Count == 0) Console.Write(' ');
                else Console.Write(lines[cli][ci++]);

                SetDefaultBGandFG();
                EnableSelectMode();
                return;
            }

            // The start and end lines are the same (cli)
            // As the highlighting direction is right, the character that will be selected is the current char
            // Example: in "l^orem", (^ is cursor pos) going to the right will highlight the o, even though the cursor is at o
            // Only one character is selected (ci), so the start and end character positions are equal to each other

            // Is left to right, not right to left
            selected_text_range = new(cli, ci, cli, ci + x, true);

            // Higlight the newly selected character
            SetSelectBGandFG();
            Console.Write(lines[cli].GetRange(ci, x).ToArray());
            ci += x;
            SetDefaultBGandFG();
            EnableSelectMode();
        }
        // Is Left to Right (towards end)
        // Lengthen selection
        else if (selected_text_range.IsLeftToRight)
        {
            // The third check in parnethesis, (selected_text_range.StartLineNumber == cli && selected_text_range.EndCharacterPosition == lines[cli].Count), is required because when the
            // cursor is on the first line of the selection, the indexing is messed up for some reason, and is 1 more than it should be. So, if the cursor is on the first line of the selection, check for if the
            // EndCharacterPosition == lines[cli].Count instead of (lines[cli].Count - 1)

            // If the range cannot be extended any further right
            if (lines[cli].Count == 0 || selected_text_range.EndCharacterPosition == lines[cli].Count - 1 || (selected_text_range.StartLineNumber == cli && selected_text_range.EndCharacterPosition == lines[cli].Count))
            {
                // Check if the current line is the last line; if it's possible to extend the selection to the next line
                if (selected_text_range.EndLineNumber == lines.Count - 1) return;

                selected_text_range.EndLineNumber++;
                cli++;
                ci = 0;
                selected_text_range.EndCharacterPosition = 0;
                Console.SetCursorPosition(ci, cli + 1); // +1 for header line
                SetSelectBGandFG();

                // If empty_line
                if (lines[cli].Count == 0) Console.Write(' ');
                else Console.Write(lines[cli][ci++]);

                SetDefaultBGandFG();
                return;
            }

            selected_text_range.EndCharacterPosition += x;

            // Rewrite newly selected character with highlight
            SetSelectBGandFG();
            Console.Write(lines[cli].GetRange(ci, x).ToArray());
            ci += x;
            SetDefaultBGandFG();
        }
        // Is Right to Left (towards start)
        // Shorten selection to the right
        else
        {
            // The range gets shortened right
            selected_text_range.StartCharacterPosition += x;

            // Rewrite newly unselected character without highlight
            SetDefaultBGandFG();
            Console.Write(lines[cli].GetRange(ci, x).ToArray());
            ci += x;
        }
    }

    private void CtrlA()
    {
        this.selected_text_range = new(0, 0, lines.Count - 1, lines[lines.Count - 1].Count - 1, true);

        // Highlight the selected text
        Console.SetCursorPosition(0, 1); // +1 for header line
        EnableSelectMode();
        Console.Write(GetNoteContent(0));
        cli = lines.Count - 1;
        ci = lines[cli].Count;
    }

    /// <summary>
    /// Ctrl+C - Copy selected text
    /// </summary>
    private void CtrlC()
    {
        string selected_text = GetSelectedContent();
        
        if (selected_text.Length != 0)
        {
            TextCopy.ClipboardService.SetText(selected_text);
        }
    }

    /// <summary>
    /// Ctrl+LeftArrow - Move one word left
    /// </summary>
    private void CtrlLeftArrow()
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
            if (cli == 0) return;

            int end_of_line = lines[--cli].Count;
            Console.CursorLeft = end_of_line;
            ci = end_of_line;
            Console.CursorTop--;
        }
    }

    /// <summary>
    /// Ctrl+Left - Move one letter left
    /// </summary>
    private void LeftArrow()
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
                Console.SetCursorPosition(ci, Console.CursorTop - 1); // -1 for header line
                cli--;
            }
        }
    }

    /// <summary>
    /// Ctrl+RightArrow - Move one word right
    /// </summary>
    private void CtrlRightArrow()
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
            if (cli == Console.BufferHeight - 3) return;

            // Move to next line if it exists or make a new one
            if (cli == lines.Count - 1)
            {
                // Create a new line
                lines.Add(new List<char>());

                // Check save
                chars_pressed += 2; // Making a new line counts as 2 chars pressed
                if (chars_pressed >= save_every)
                {
                    states.AddState(lines, Tuple.Create(ci, cli));
                    chars_pressed = 0;
                }
            }

            // Move to the next line
            ci = 0;
            cli++;
            Console.SetCursorPosition(0, Console.CursorTop + 1); // +1 for header line
        }
    }

    /// <summary>
    /// Ctrl+Right - Move one letter right
    /// </summary>
    private void RightArrow()
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
            if (cli == Console.BufferHeight - 3) return;

            // Move to next line if it exists or make a new one
            if (cli == lines.Count - 1)
            {
                // Create a new line
                lines.Add(new List<char>());

                // Check save
                chars_pressed += 2; // Making a new line counts as 2 chars pressed
                if (chars_pressed >= save_every)
                {
                    states.AddState(lines, Tuple.Create(ci, cli));
                    chars_pressed = 0;
                }
            }

            // Move to the next line
            ci = 0;
            cli++;
            Console.SetCursorPosition(0, Console.CursorTop + 1); // +1 for header line
        }
    }

    /// <summary>
    /// DownArrow key - Move one line down
    /// </summary>
    private void DownArrow()
    {
        /***
            * IMPORTANT: if the cursor is at the bottom of the screen
            * do not allow any text to be written
            * 
            * The user must expand their console to write more
            ***/
        if (cli == Console.BufferHeight - 3) return;

        // Move to next line if it exists or make a new one then move to it
        if (cli + 1 == lines.Count)
        {
            // Create a new line
            lines.Add(new List<char>());

            // Check save
            chars_pressed += 2; // Making a new line counts as 2 chars pressed
            if (chars_pressed >= save_every)
            {
                states.AddState(lines, Tuple.Create(ci, cli));
                chars_pressed = 0;
            }
        }

        // Move to the next line
        int left_pos = (Console.CursorLeft < lines[++cli].Count) ? Console.CursorLeft : lines[cli].Count;
        ci = left_pos;
        Console.SetCursorPosition(left_pos, Console.CursorTop + 1); // +1 for header line
    }

    /// <summary>
    /// UpArrow key - Move one line up 
    /// </summary>
    private void UpArrow()
    {
        // If the current line is not the first line
        if (cli > 0)
        {
            int left_pos = (Console.CursorLeft < lines[--cli].Count) ? Console.CursorLeft : lines[cli].Count;
            ci = left_pos;
            Console.SetCursorPosition(left_pos, Console.CursorTop - 1); // +1 for header line
        }
    }

    /// <summary>
    /// Ctrl+Backspace - delete the word to the left of the cursor (delete until the last space)
    /// </summary>
    private void CtrlBackspace()
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

            // Save new state - an entire word was deleted
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
        // If the cursor is at the very beginning, the current line should be appended to the previous line if there is any text in front of the cursor
        // Otherwise, the cursor should just move to the previous line
        else
        {
            // Nowhere to go if on the first line
            if (cli == 0) return;

            /***
             * IMPORTANT: If the line has too many characters in it, overflow
             * will be prevented by writing the characters that fit and leaving
             * the rest on the current line.
             * 
             * If there is no text in the line, move the cursor to the end of the
             * previous line and delete the line.
             ***/
            if (lines[cli].Count != 0 && lines[cli - 1].Count >= Console.BufferWidth - 2) return;

            // Used for placing the cursor
            int prev_line_length = lines[cli - 1].Count;

            // Append as much as possible from this line to the one before it
            if (lines[cli].Count + lines[cli - 1].Count >= Console.BufferWidth - 2)
            {
                // If the line is too long, only append as much as possible
                int amount_of_chars_to_append = Console.BufferWidth - 2 - lines[cli - 1].Count;
                List<char> chars_to_append = lines[cli].GetRange(0, amount_of_chars_to_append);
                lines[cli - 1].AddRange(chars_to_append);
                lines[cli].RemoveRange(0, amount_of_chars_to_append);

                // Rewrite current line (that was deleted from) - rewrite the remaining chars
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.BufferWidth));
                Console.CursorLeft = 0;
                Console.Write(lines[cli].ToArray());

                // Rewrite top line (where text was added to)
                Console.CursorTop--;
                // Set cursor to end of line
                Console.CursorLeft = lines[--cli].Count - amount_of_chars_to_append;
                Console.Write(chars_to_append.ToArray());
                Console.CursorLeft -= amount_of_chars_to_append;
                ci = Console.CursorLeft;
            }
            else
            {
                // Append the entire line
                lines[cli - 1].AddRange(lines[cli]);

                // Delete the line
                lines.RemoveAt(cli--);

                /*** Update the screen ***/

                // Remember the position to go to
                int cursor_pos = Console.CursorTop - 1;
                ci = prev_line_length;

                // There is too much text to wipe (would be every line below the current), so the best move is to clear everything and rewrite
                int[] start_rewriting_from = new int[2] { Console.CursorLeft, Console.CursorTop - 1 }; // Rewrite the current line as well (top - 1), -1 for header line
                Console.Write(new string(' ', (lines.Count - cli) * Console.BufferWidth));
                Console.SetCursorPosition(start_rewriting_from[0], start_rewriting_from[1]);

                Console.Write(GetNoteContent(cli));
                Console.SetCursorPosition(ci, cursor_pos);
            }

            // Save new state - pretty big change (moving the lines)
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
    }

    /// <summary>
    /// Backspace key - delete the character to the left of the cursor
    /// </summary>
    private void Backspace()
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

                // Check save
                if (chars_pressed == 0)
                {
                    chars_pressed += 2; // Two chars were changed
                    if (chars_pressed >= save_every)
                    {
                        states.AddState(lines, Tuple.Create(ci, cli));
                        chars_pressed = 0;
                    }
                }
                else
                {
                    // This delete simply undid a change, so chars_pressed should be decremented by 2 to even out the chars_pressed_tracker
                    chars_pressed -= 2; // Two chars were changed
                }
            }
            else
            {
                lines[cli].RemoveAt(--ci);

                // Rewrite line without char
                int previous_cursor_pos = Console.CursorLeft;
                Console.CursorLeft = 0;
                Console.Write(new string(lines[cli].ToArray()) + " ");
                Console.CursorLeft = previous_cursor_pos - 1;

                // Check save
                if (chars_pressed == 0)
                {
                    chars_pressed++;
                    if (chars_pressed >= save_every)
                    {
                        states.AddState(lines, Tuple.Create(ci, cli));
                        chars_pressed = 0;
                    }
                }
                else
                {
                    // This delete simply undid a change, so chars_pressed should be decremented to even out the chars_pressed_tracker
                    chars_pressed--;
                }
            }
        }
        // Index is at the beginning, so the current line must be moved forward (up the screen) by one
        else
        {
            // Nowhere to go if on the first line
            if (cli == 0) return;

            /***
             * IMPORTANT: If the line has too many characters in it, overflow
             * will be prevented by writing the characters that fit and leaving
             * the rest on the current line.
             * 
             * If there is no text in the line, move the cursor to the end of the
             * previous line and delete the line.
             ***/
            if (lines[cli].Count != 0 && lines[cli - 1].Count >= Console.BufferWidth - 2) return;

            // Used for placing the cursor
            int prev_line_length = lines[cli - 1].Count;

            // Append as much as possible from this line to the one before it
            if (lines[cli].Count + lines[cli - 1].Count >= Console.BufferWidth - 2)
            {
                // If the line is too long, only append as much as possible
                int amount_of_chars_to_append = Console.BufferWidth - 2 - lines[cli - 1].Count;
                List<char> chars_to_append = lines[cli].GetRange(0, amount_of_chars_to_append);
                lines[cli - 1].AddRange(chars_to_append);
                lines[cli].RemoveRange(0, amount_of_chars_to_append);

                // Rewrite current line (that was deleted from) - rewrite the remaining chars
                Console.CursorLeft = 0;
                Console.Write(new string(' ', Console.BufferWidth));
                Console.CursorLeft = 0;
                Console.Write(lines[cli].ToArray());

                // Rewrite top line (where text was added to)
                Console.CursorTop--;
                // Set cursor to end of line
                Console.CursorLeft = lines[--cli].Count - amount_of_chars_to_append;
                Console.Write(chars_to_append.ToArray());
                Console.CursorLeft -= amount_of_chars_to_append;
                ci = Console.CursorLeft;
            }
            else
            {
                // Append the entire line
                lines[cli - 1].AddRange(lines[cli]);

                // Delete the line
                lines.RemoveAt(cli--);

                /*** Update the screen ***/

                // Remember the position to go to
                int cursor_pos = Console.CursorTop - 1;
                ci = prev_line_length;

                // There is too much text to wipe, so the best move is to clear everything and rewrite
                int[] start_rewriting_from = new int[2] { Console.CursorLeft, Console.CursorTop - 1 }; // Rewrite the current line as well (top - 1), -1 for header line
                Console.Write(new string(' ', (lines.Count - cli) * Console.BufferWidth));
                Console.SetCursorPosition(start_rewriting_from[0], start_rewriting_from[1]);

                Console.Write(GetNoteContent(cli));
                Console.SetCursorPosition(ci, cursor_pos);
            }
            
            // Save new state - pretty big change (moving the lines)
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
    }

    /// <summary>
    /// Ctrl+Delete - delete the word to the right of the cursor (delete until next space)
    /// </summary>
    private void CtrlDelete()
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

            // Save new state - an entire word was deleted
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
        // If the cursor is at the very end of the line, the following (next) line should be appended to the current line
        else
        {
            /***
             * IMPORTANT: If the line has too many characters in it, overflow
             * will be prevented by blocking new characters from being written
             ***/
            if (lines[cli].Count + lines[cli + 1].Count >= Console.BufferWidth - 2) return;

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
                Console.SetCursorPosition(0, Console.CursorTop + 1); // +1 for header line
                Console.Write(new string(' ', Console.BufferWidth));
            }

            Console.SetCursorPosition(start_rewriting_from[0], start_rewriting_from[1]);

            Console.CursorLeft = 0;
            Console.Write(GetNoteContent(cli));
            Console.SetCursorPosition(c_left, c_top);

            // Save new state - pretty big change (moving the lines)
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
    }

    /// <summary>
    /// Delete key - delete the character to the right of the cursor
    /// </summary>
    private void Delete()
    {
        // If at the end of the line the next line must be appended to this one (deleting the \n that separates the two)
        if (ci == lines[cli].Count)
        {
            // Can't do this on the last line
            if (cli == lines.Count - 1) return;

            /***
             * IMPORTANT: If the line has too many characters in it, overflow
             * will be prevented by blocking new characters from being written
             ***/
            if (lines[cli].Count + lines[cli + 1].Count >= Console.BufferWidth - 2) return;

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
                Console.SetCursorPosition(0, Console.CursorTop + 1); // +1 for header line
                Console.Write(new string(' ', Console.BufferWidth));
            }

            Console.SetCursorPosition(start_rewriting_from[0], start_rewriting_from[1]);

            Console.CursorLeft = 0;
            Console.Write(GetNoteContent(cli));
            Console.SetCursorPosition(c_left, c_top);

            // Save new state - pretty big change (moving the lines)
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
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

            // Check save
            if (chars_pressed == 0)
            {
                chars_pressed++;
                if (chars_pressed >= save_every)
                {
                    states.AddState(lines, Tuple.Create(ci, cli));
                    chars_pressed = 0;
                }
            }
            else
            {
                // This delete simply undid a change, so chars_pressed should be decremented to even out the chars_pressed_tracker
                chars_pressed--;
            }
        }
    }

    /// <summary>
    /// Ctrl+Shift+K - delete the current line
    /// </summary>
    private void CtrlShiftK()
    {
        /*** Deletes the current line ***/

        // If there are no lines to delete
        if (lines.Count == 0) return;

        // Add the new line
        lines.RemoveAt(cli--);
        ci = lines[cli].Count;

        // Clear screen
        Console.SetCursorPosition(0, 1);
        for (int i = 0; i < lines.Count + 1; i++)
        {
            Console.WriteLine(new string(' ', Console.BufferWidth));
        }

        // Rewrite note
        Console.SetCursorPosition(0, 1);
        for (int i = 0; i < lines.Count; i++)
        {
            Console.WriteLine(new string(lines[i].ToArray()));
        }

        // Reset cursor loc
        Console.SetCursorPosition(ci, cli + 1); // +1 for header line

        // Check save
        chars_pressed += 2; // Deleting the line counts as 2 chars pressed
        if (chars_pressed >= save_every)
        {
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
    }

    /// <summary>
    /// Ctrl+/ - Add closing markup tag
    /// </summary>
    private void CtrlForwardSlash()
    {
        /***
            * IMPORTANT: If the line has too many characters in it, overflow
            * will be prevented by blocking new characters from being written
            ***/
        if (lines[cli].Count + 3 >= Console.BufferWidth - 2) return;

        // If cursor is inside opening markup tag, add the closing tag to the end
        // Ex: [green<cursor_here>] -> <Ctrl+/ pressed> -> [green]<cursor_here>[/]
        while (lines[cli].Count > ci && lines[cli][ci] == ']')
        {
            ci++;
            Console.CursorLeft++;
        }

        // Insert the closing tag
        lines[cli].InsertRange(ci, "[/]");

        // Rewrite line
        int previous_loc = ci;
        Console.Write(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray());

        // Move cursor
        Console.CursorLeft = previous_loc;
        ci = previous_loc;

        // Save new state - a chunk of chars was added
        states.AddState(lines, Tuple.Create(ci, cli));
        chars_pressed = 0;
    }

    /// <summary>
    /// For when [, (, or { is pressed - the corresponding closing bracket will be added as well
    /// <br/><br/>
    /// Add ending bracket when pressing the opening bracket [ => []
    /// Add ending bracket when pressing the opening bracket ( => ()
    /// Add ending bracket when pressing the opening bracket { => {}
    /// </summary>
    /// <param name="bracket_type">The type of closing bracket to add: ] = 0, ) = 1, } = 2</param>
    private void ClosingBracket(int bracket_type)
    {
        /***
         * IMPORTANT: If the line has too many characters in it, overflow
         * will be prevented by blocking new characters from being written
        ***/
        if (lines[cli].Count + 2 >= Console.BufferWidth - 2) return;

        /*** Insert the two brackets ***/

        // Add closing breacket
        if (bracket_type == 0)
        {
            lines[cli].InsertRange(ci, "[]");
        }
        else if (bracket_type == 1)
        {
            lines[cli].InsertRange(ci, "()");
        }
        else if (bracket_type == 2)
        {
            lines[cli].InsertRange(ci, "{}");
        }

        // Rewrite line
        int previous_loc = ci;
        Console.Write(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray());

        // Move cursor to inside the brackets
        Console.CursorLeft = previous_loc + 1;
        ci = previous_loc + 1;

        // Check save
        chars_pressed += 2; // Two chars were added
        if (chars_pressed >= save_every)
        {
            states.AddState(lines, Tuple.Create(ci, cli));
            chars_pressed = 0;
        }
    }

    /// <summary>
    /// Handle markup shortcuts such as Ctrl+I or Ctrl+B
    /// <br/><br/>
    /// All keys checked in the switch statement have the <see cref="ConsoleModifiers.Control"/> flag,
    /// as the keyevent must have this flag in the <see cref="this.MainLoop"/> for this function to be called
    /// </summary>
    /// <param name="keyinfo"></param>
    private void HandleMarkupKeybinds(ConsoleKeyInfo keyinfo)
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
                // Bold - Ctrl+B
                if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    _chars_to_add.AddRange("[rapidblink][/]");
                }
                // Blink - Ctrl+Shift+B
                else
                {
                    _chars_to_add.AddRange("[bold][/]");
                }
                break;

            // Underline - Ctrl+U
            case ConsoleKey.U:
                _chars_to_add.AddRange("[underline][/]");
                break;

            // Strikethrough - Ctrl+Shift+5
            case ConsoleKey.D5:
                // Strikethrough - Ctrl+Shift+5
                if (keyinfo.Modifiers.HasFlag(ConsoleModifiers.Shift))
                {
                    _chars_to_add.AddRange("[strikethrough][/]");
                }
                // User Color5 - Ctrl+5
                else
                {
                    _chars_to_add.AddRange($"[{Program.Settings.Color5}][/]");
                }
                break;

            // Dim - Ctrl+D
            case ConsoleKey.D:
                _chars_to_add.AddRange("[dim][/]");
                break;

            // User Color1 - Ctrl+1
            case ConsoleKey.D1:
                _chars_to_add.AddRange($"[#{Program.Settings.Color1}][/]");
                break;

            // User Color2 - Ctrl+2
            case ConsoleKey.D2:
                _chars_to_add.AddRange($"[#{Program.Settings.Color2}][/]");
                break;

            // User Color3 - Ctrl+3
            case ConsoleKey.D3:
                _chars_to_add.AddRange($"[#{Program.Settings.Color3}][/]");
                break;

            // User Color4 - Ctrl+4
            case ConsoleKey.D4:
                _chars_to_add.AddRange($"[#{Program.Settings.Color4}][/]");
                break;

            // User Color5 - Ctrl+5 is under the strikethrough method above (Ctrl+Shift+5)

            // User Color6 - Ctrl+6
            case ConsoleKey.D6:
                _chars_to_add.AddRange($"[#{Program.Settings.Color6}][/]");
                break;

            // User Color7 - Ctrl+7
            case ConsoleKey.D7:
                _chars_to_add.AddRange($"[#{Program.Settings.Color7}][/]");
                break;

            // User Color8 - Ctrl+8
            case ConsoleKey.D8:
                _chars_to_add.AddRange($"[#{Program.Settings.Color8}][/]");
                break;

            // User Color9 - Ctrl+9
            case ConsoleKey.D9:
                _chars_to_add.AddRange($"[#{Program.Settings.Color9}][/]");
                break;

            // User Color0 - Ctrl+0
            case ConsoleKey.D0:
                _chars_to_add.AddRange($"[#{Program.Settings.Color0}][/]");
                break;

            // No special keybind preseed
            default: return;
        }

        /***
         * IMPORTANT: If the line has too many characters in it, overflow
         * will be prevented by blocking new characters from being written
         ***/
        if (lines[cli].Count + _chars_to_add.Count >= Console.BufferWidth - 2) return;

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

        // Save new state - a chunk of chars was added
        states.AddState(lines, Tuple.Create(ci, cli));
        chars_pressed = 0;
    }

    /// <summary>
    /// Tab key - special check - Move the cursor just outside the closing markup tag [/] if the cursor was between a set of markup tags when Tab was pressed
    /// </summary>
    /// <returns>A success value indicating whether the cursor was between a set of markup tags</returns>
    private bool Tab()
    {
        // Check if the key was TAB and if the cursor is within a set of markup tags [italic]cursor here[/],
        // move the cursor to just outside the closing tag [/]

        // This is done by finding both an opening and ending tag to the right of the 'ci' if one exists
        // If the [/] (closing tag) is closer than the opening tag, the 'ci' must be inside the markup
        string search_range = new string(lines[cli].GetRange(ci, lines[cli].Count - ci).ToArray())!;

        if (!search_range.Contains("[/]")) return false;
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
            return true;
        }

        return false;
    }

    /// <summary>
    /// Set Console.BackgroundColor and Console.ForegroundColor to the default colors (white FG, black BG)
    /// </summary>
    private void SetDefaultBGandFG()
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
    }

    /// <summary>
    /// Set Console.BackgroundColor and Console.ForegroundColor to the selected/highlighted text colors (black FG, white BG)
    /// </summary>
    private void SetSelectBGandFG()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
    }

    private bool select_mode = false;

    /// <summary>
    /// Enable highlight mode, meaning the colors background and foreground colors are inverted
    /// </summary>
    private void EnableSelectMode()
    {
        Console.BackgroundColor = ConsoleColor.White;
        Console.ForegroundColor = ConsoleColor.Black;
        select_mode = true;
    }

    /// <summary>
    /// Disable highlight mode, meaning:<br/>
    /// - the colors background and foreground colors are normal<br/>
    /// - all text is unselected/unhighlighted, which is done by rewriting the note
    /// </summary>
    private void DeselectText()
    {
        // Current position
        int left = Console.CursorLeft;
        int top = Console.CursorTop;

        // Change colors back to normal
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        select_mode = false;

        // Unselect text by rewriting
        Console.SetCursorPosition(0, 1);
        Console.Write(GetNoteContent(0).Replace("\n", " \n"));
        Console.SetCursorPosition(left, top);

        // Clear the selection
        selected_text_range = new(-1, -1, -1, -1, false);
    }

    private void DeleteSelectedText()
    {
        this.DeselectText();
        throw new NotImplementedException(); // TODO: Implement
    }

    /// <summary>
    /// Find the amount of characters in the left direction to get to the next space character or to get to the start of the line (EOL)
    /// </summary>
    /// <param name="_cli">The index of the current line to look at</param>
    /// <param name="_ci">The index of the current character in the cli to start looking from</param>
    /// <returns>The amount of characters to get to the next space character or the start of the line (EOL), going left</returns>
    private int CharsLeftUntillNextSpaceOrEOL(int _cli, int _ci)
    {
        if (_ci == 0) return 0;

        int x = 0;
        int search_buffer = 0;
        while (x == 0)
        {
            for (int i = _ci - 1 - search_buffer; i >= 0; i--)
            {
                if (lines[_cli][i] == ' ') break;
                x++;
            }

            search_buffer++;
        }

        return x + search_buffer - 1;
    }

    /// <summary>
    /// Find the amount of characters in the right direction to get to the next space character or to get to the end of the line (EOL)
    /// </summary>
    /// <param name="_cli">The index of the current line to look at</param>
    /// <param name="_ci">The index of the current character in the cli to start looking from</param>
    /// <returns>The amount of characters to get to the next space character or the end of the line (EOL), going right</returns>
    private int CharsRightUntilNextSpaceOrEOL(int _cli, int _ci)
    {
        if (_ci == lines[_cli].Count) return 0;

        int x = 0;
        int search_buffer = 0;
        while (x == 0)
        {
            for (int i = _ci + 1 + search_buffer; i < lines[_cli].Count; i++)
            {
                if (lines[_cli][i] == ' ') break;
                x++;
            }

            search_buffer++;
        }

        return x + search_buffer;
    }
}
