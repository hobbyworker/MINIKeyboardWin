namespace MINIKeyboard.Views;

/// <summary>What the app is called and where it lives, given by the composition root.</summary>
/// <param name="IconPath">The app icon as a PNG, for the title bar and About; null draws none.</param>
public sealed record AppInfo(
    string Name, string Version, string? Copyright, string? Website, string? IssueUrl, string? IconPath = null);
