namespace ConsoleNotes;

internal enum Mode
{
    /// <summary>
    /// For when the user is viewing his previously written notes
    /// Enables the up and down arrow keys for scrolling through the notes
    /// </summary>
    ViewNotes,
    /// <summary>
    /// For when the user is selecting a note to edit
    /// </summary>
    EditNote,
    /// <summary>
    /// For when the user is selecting a note to delete
    /// </summary>
    DeleteNote,
    /// <summary>
    /// For when the user is in the new note menu, but is not writing the note yet
    /// </summary>
    NewNote
}