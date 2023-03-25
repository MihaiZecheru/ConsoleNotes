namespace ConsoleNotes;

internal struct NotesRange
{
    public int Start { get; set; }
    public int End { get; set; }

    public NotesRange(int start, int end)
    {
        Start = start;
        End = end;
    }
}