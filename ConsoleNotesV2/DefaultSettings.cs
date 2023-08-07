using Spectre.Console;

namespace ConsoleNotes;

internal static class DefaultSettings
{
    public static bool ShowRainbowNotes { get; } = true;
    public static bool NotesDisplayOrder_NewestFirst { get; } = true;
    public static bool DateDayFirst { get; } = false;
    public static string Color1 { get; } = Color.Tomato.ToHex();
    public static string Color2 { get; } = Color.Orange.ToHex();
    public static string Color3 { get; } = Color.Yellow.ToHex();
    public static string Color4 { get; } = Color.SeaGreen.ToHex();
    public static string Color5 { get; } = Color.DeepSkyBlue3_1.ToHex();
    public static string Color6 { get; } = Color.Indigo.ToHex();
    public static string Color7 { get; } = Color.DeepPink.ToHex();
    public static string Color8 { get; } = Color.Cyan.ToHex();
    public static string Color9 { get; } = Color.MediumPurple.ToHex();
    public static string Color0 { get; } = Color.CornflowerBlue.ToHex();
}
