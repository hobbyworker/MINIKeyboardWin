namespace MINIKeyboard.Views;

/// <summary>What the app is called and where it lives, given by the composition root.</summary>
/// <param name="IconPath">The app icon as a PNG, for the title bar and About; null draws none.</param>
/// <param name="Licenses">The licenses of the software the app comes with, for About; none, no such item.</param>
public sealed record AppInfo(
    string Name,
    string Version,
    string? Copyright,
    string? Website,
    string? IssueUrl,
    string? IconPath = null,
    IReadOnlyList<LicenseNotice>? Licenses = null);

/// <summary>A piece of software the app comes with: its name, where it comes from, and its license text.</summary>
public sealed record LicenseNotice(string Name, string? Url, string Text);
