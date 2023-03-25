namespace ConsoleNotes;

internal class CtrlZ
{
    /// <summary>
    /// The maximum amount of states that can be saved before the oldest state is cleared
    /// </summary>
    private static int MAX_STATES = 50;

    /// <summary>
    /// The state that is currently being used
    /// <br></br><br></br>
    /// Ctrl+Z to undo will move this index back one
    /// Ctrl+Y to redo will move this index forward one
    /// </summary>
    private int ActiveIndex = 0;

    /// <summary>
    /// The list of states that have been saved
    /// <br></br><br></br>
    /// Each state is the entire note (List of List of chars)
    /// </summary>
    private List<List<List<char>>> NoteStates = new List<List<List<char>>>();

    /// <summary>
    /// Keeps track of where the cursor was when the state was saved
    /// </summary>
    private List<Tuple<int, int>> CursorStates = new List<Tuple<int, int>>();

    /// <summary>
    /// Keep track of the changes made to a note
    /// </summary>
    public CtrlZ()
    {
        // Add first state - empty note
        NoteStates.Add(new List<List<char>>());
        CursorStates.Add(new Tuple<int, int>(0, 0));
    }

    /// <summary>
    /// Update the current state of the note
    /// </summary>
    /// <param name="note">The updated note</param>
    public void AddState(List<List<char>> note, Tuple<int, int> cursor_pos)
    {
        // Check if state should be added to the end
        if (ActiveIndex == NoteStates.Count - 1)
        {
            NoteStates.Add(note);
            CursorStates.Add(cursor_pos);
            ActiveIndex++;
        }
        else
        {
            // Otherwise, the new state will be treated as the most recent one (because it is)
            // And therefore, all the states following the current one will be removed
            NoteStates = NoteStates.GetRange(0, ActiveIndex);
            
            // Add the new state
            NoteStates.Add(note);
            CursorStates.Add(cursor_pos);
            ActiveIndex++;
        }

        // If after the new state was added there are more than MAX_STATES amount of states
        // Remove the oldest state from memory
        if (NoteStates.Count > MAX_STATES)
        {
            NoteStates.RemoveAt(0);
            CursorStates.RemoveAt(0);
            ActiveIndex--;
        }
    }

    /// <summary>
    /// Undo the last change by reverting to the previous state (ActiveIndex - 1)
    /// </summary>
    /// <returns>The previous state (ActiveIndex - 1) and the cursor position from that state</returns>
    public Tuple<List<List<char>>, Tuple<int, int>> Undo()
    {
        // If the ActiveIndex is at the very beginning of the list,
        // Then the current state is the most recent edition of the note
        // And there is nowhere to Undo to, therefore, the current state is returend
        if (ActiveIndex == 0)
        {
            return Tuple.Create(NoteStates[ActiveIndex], CursorStates[ActiveIndex]);
        }
        // Otherwise, the previous state will be returned (ActiveIndex - 1)
        else
        {
            return Tuple.Create(NoteStates[--ActiveIndex], CursorStates[ActiveIndex]);
        }
    }

    /// <summary>
    /// Redo essentially means to Undo a previous Undo
    /// <br></br><br></br>
    /// This will move to the state which follows the current state (ActiveIndex + 1)
    /// </summary>
    /// <returns>The state which follows the current state (ActiveIndex + 1) and the cursor position from that state</returns>
    public Tuple<List<List<char>>, Tuple<int, int>> Redo()
    {
        // If the ActiveIndex is at the end of the list,
        // there is no state which follows the current one,
        // meaning the current one is the most recent state
        // Therefore, the current state will be returned
        if (ActiveIndex == NoteStates.Count - 1)
        {
            return Tuple.Create(NoteStates[ActiveIndex], CursorStates[ActiveIndex]);
        }
        // Otherwise, return the state which follows the current state (ActiveIndex + 1)
        else
        {
            return Tuple.Create(NoteStates[++ActiveIndex], CursorStates[ActiveIndex]);
        }
    }
}
