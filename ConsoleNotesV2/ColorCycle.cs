namespace ConsoleNotes;

/// <summary>
/// Cycle through a series of Colors from <see cref="Spectre.Console.Color"/>
/// <br></br><br></br>
/// Each color is repeated twice to make the transition smoother
/// </summary>
internal static class ColorCycle
{
    // Capping the cycle at 13 prevents the notes from changing their color as the user scrolls as there are 11 notes displayed at a time
    /// <summary>
    /// Amount of colors in the cycle
    /// </summary>
    private static int ColorsCount = 13;

    /// <summary>
    /// The current index the cycle is at
    /// </summary>
    private static int Index = 0;

    /// <summary>
    /// The list of colors to cycle through
    /// </summary>
    private static List<Spectre.Console.Color> Cycle = new List<Spectre.Console.Color>()
    {
        Spectre.Console.Color.Maroon,
        Spectre.Console.Color.Maroon,
        Spectre.Console.Color.DarkOrange3_1,
        Spectre.Console.Color.DarkOrange3_1,
        Spectre.Console.Color.Gold3_1,
        Spectre.Console.Color.Gold3_1,
        Spectre.Console.Color.Chartreuse2_1,
        Spectre.Console.Color.Chartreuse2_1,
        Spectre.Console.Color.Lime,
        Spectre.Console.Color.Lime,
        Spectre.Console.Color.DarkCyan,
        Spectre.Console.Color.DarkCyan,
        Spectre.Console.Color.DeepSkyBlue2,
        Spectre.Console.Color.DeepSkyBlue2,
        Spectre.Console.Color.DodgerBlue2,
        Spectre.Console.Color.DodgerBlue2,
        Spectre.Console.Color.RoyalBlue1,
        Spectre.Console.Color.RoyalBlue1,
        Spectre.Console.Color.DarkViolet_1,
        Spectre.Console.Color.DarkViolet_1,
        Spectre.Console.Color.MediumOrchid3,
        Spectre.Console.Color.MediumOrchid3
    };

    /// <summary>
    /// Cycle the colors once and get the new color
    /// </summary>
    /// <returns>The next color in the cycle</returns>
    public static Spectre.Console.Color CycleNext()
    {
        if (Index == ColorsCount - 1) Index = 0;
        return Cycle[Index++];
    }
}