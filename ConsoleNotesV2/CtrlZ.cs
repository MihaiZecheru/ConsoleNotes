namespace ConsoleNotes;

internal class CtrlZ
{
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
    private List<List<List<char>>> States = new List<List<List<char>>>();

    /// <summary>
    /// Keep track of the changes made to a note
    /// </summary>
    public CtrlZ()
    {
        // Add first state - empty note
        States.Add(new List<List<char>>());
    }

    /// <summary>
    /// Update the current state of the note
    /// </summary>
    /// <param name="note">The updated note</param>
    public void AddState(List<List<char>> note)
    {
        // Check if state should be added to the end
        if (ActiveIndex == States.Count - 1)
        {
            States.Add(note);
            ActiveIndex++;
        }
        else
        {
            // Otherwise, the new state will be treated as the most recent one (because it is)
            // And therefore, all the states after will be removed
            States = States.GetRange(0, ActiveIndex);
            States.Add(note);
            ActiveIndex++;
        }

        // If after the new state was added there are more than MAX_STATES amount of states
        // Remove the oldest state from memory
        if (States.Count > MAX_STATES)
        {
            States.RemoveAt(0);
            ActiveIndex--;
        }
    }

    /// <summary>
    /// Undo the last change by reverting to the previous state (ActiveIndex - 1)
    /// </summary>
    /// <returns>The previous state (ActiveIndex - 1)</returns>
    public List<List<char>> Undo()
    {
        // If the ActiveIndex is at the very beginning of the list,
        // Then the current state is the most recent edition of the note
        // And there is nowhere to Undo to, therefore, the current state is returend
        if (ActiveIndex == 0)
        {
            return States[ActiveIndex];
        }
        // Otherwise, the previous state will be returned (ActiveIndex - 1)
        else
        {
            return States[--ActiveIndex];
        }
    }

    /// <summary>
    /// Redo essentially means to Undo a previous Undo
    /// <br></br><br></br>
    /// This will move to the state which follows the current state (ActiveIndex + 1)
    /// </summary>
    /// <returns>The state which follows the current state</returns>
    public List<List<char>> Redo()
    {
        // If the ActiveIndex is at the end of the list,
        // there is no state which follows the current one,
        // meaning the current one is the most recent state
        // Therefore, the current state will be returned
        if (ActiveIndex == States.Count - 1)
        {
            return States[ActiveIndex];
        }
        // Otherwise, return the state which follows the current state (ActiveIndex + 1)
        else
        {
            return States[++ActiveIndex];
        }
    }
}
