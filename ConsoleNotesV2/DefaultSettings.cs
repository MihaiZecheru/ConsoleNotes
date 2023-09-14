using Spectre.Console;

namespace ConsoleNotes;

internal static class DefaultSettings
{
    public static bool ShowRainbowNotes { get; } = true;
    public static bool NotesDisplayOrder_NewestFirst { get; } = true;
    public static bool DateDayFirst { get; } = false;
    public static string Color1 { get; } = Color.IndianRed1.ToHex();
    public static string Color2 { get; } = Color.SandyBrown.ToHex();
    public static string Color3 { get; } = Color.Yellow.ToHex();
    public static string Color4 { get; } = Color.SeaGreen2.ToHex();
    public static string Color5 { get; } = Color.DeepSkyBlue3_1.ToHex();
    public static string Color6 { get; } = Color.Wheat1.ToHex();
    public static string Color7 { get; } = Color.DeepPink2.ToHex();
    public static string Color8 { get; } = Color.Cyan1.ToHex();
    public static string Color9 { get; } = Color.MediumPurple.ToHex();
    public static string Color0 { get; } = Color.CornflowerBlue.ToHex();
}
