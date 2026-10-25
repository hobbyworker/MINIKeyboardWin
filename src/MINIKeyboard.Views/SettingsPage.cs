using System.Diagnostics;
using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>
/// Settings, as a page of the main window — the way the Windows 11 apps (Photos, Notepad, the Store) lay theirs out: a
/// large title, sections, and a card for each setting with its icon, name, a line on what it does, and its control on
/// the right. What the app sends a keyboard, its updates (in a build that has them), the interface's language and light
/// or dark, and About last.
/// </summary>
internal sealed class SettingsPage
{
    private readonly ScrollViewer scroller = new() { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
    private readonly SettingsExpander about;

    /// <param name="hasUpdates">A build with updates of its own: the App section, for whether it looks by itself.</param>
    /// <param name="showLicenses">Opens the licenses of the software the app comes with.</param>
    public SettingsPage(
        InterfaceSettings settings, AppInfo info, Action<Appearance> applyAppearance, bool hasUpdates,
        Action showLicenses)
    {
        var choices = settings.Choices;
        var page = new StackPanel
        {
            Spacing = 4,
            MaxWidth = 1000,
            Margin = new Thickness(36, 8, 36, 36),
        };
        page.Children.Add(new TextBlock
        {
            Text = T("Settings"),
            Style = Styles.Get("TitleLargeTextBlockStyle"),
            Margin = new Thickness(0, 0, 0, 12),
        });

        // What the app sends a keyboard to work around its firmware's bugs.
        page.Children.Add(SectionHeader(T("Firmware Workarounds")));
        var repeat = new ToggleSwitch { IsOn = choices.PreventLightingKeyRepeat };
        repeat.Toggled += (_, _) => settings.Update(current => current with { PreventLightingKeyRepeat = repeat.IsOn });
        page.Children.Add(Card(
            Glyphs.Keyboard,
            T("Prevent key repeat after changing the lighting"),
            T("After its lighting is saved, the 1189:8890 keyboard repeats a key's first stroke without end when the key is pressed (its maker's software does the same). With this on, the app sends one more save after the lighting to stop that; the keys stay as they were. Off, the app sends just what the maker's software sends."),
            repeat));

        if (hasUpdates)
        {
            page.Children.Add(SectionHeader(T("App")));
            var automatic = new ToggleSwitch { IsOn = choices.CheckForUpdates };
            automatic.Toggled += (_, _) => settings.Update(current => current with { CheckForUpdates = automatic.IsOn });
            page.Children.Add(Card(Glyphs.Sync, T("Check for updates automatically"), null, automatic));
        }

        // The interface.
        page.Children.Add(SectionHeader(T("Language & Appearance")));
        var language = new ComboBox { MinWidth = 200 };
        language.Items.Add(T("Follow System"));
        foreach (var (_, name) in InterfaceSettings.Languages)
            language.Items.Add(name);
        language.SelectedIndex = InterfaceSettings.Languages.ToList().FindIndex(entry => entry.Code == choices.Language) + 1;
        page.Children.Add(Card(
            Glyphs.Globe,
            T("Language"),
            T("The base language is English. You can follow the system language or choose one yourself."),
            language));

        var appearance = new ComboBox { MinWidth = 200 };
        appearance.Items.Add(T("Follow System"));
        appearance.Items.Add(T("Light"));
        appearance.Items.Add(T("Dark"));
        appearance.SelectedIndex = (int)choices.Appearance;
        page.Children.Add(Card(Glyphs.Palette, T("Appearance"), null, appearance));

        // The language is read once, at launch.
        var relaunch = new InfoBar
        {
            Title = T("Language Changed"),
            Message = T("%@ needs to relaunch to apply the change.", info.Name),
            Severity = InfoBarSeverity.Informational,
            IsOpen = false,
        };
        var relaunchNow = new Button { Content = T("Relaunch Now") };
        relaunchNow.Click += (_, _) => Relaunch();
        relaunch.ActionButton = relaunchNow;
        page.Children.Add(relaunch);

        language.SelectionChanged += (_, _) =>
        {
            var index = language.SelectedIndex;
            var code = index <= 0 ? null : InterfaceSettings.Languages[index - 1].Code;
            settings.Update(current => current with { Language = code });
            relaunch.IsOpen = true;
        };
        appearance.SelectionChanged += (_, _) =>
        {
            var chosen = (Appearance)Math.Max(0, appearance.SelectedIndex);
            settings.Update(current => current with { Appearance = chosen });
            applyAppearance(chosen);
        };

        // About, as the Store shows it: the app on a card that opens to its links.
        page.Children.Add(SectionHeader(T("About")));
        about = new SettingsExpander
        {
            Header = info.Name,
            Description = info.Copyright is { } copyright ? $"{copyright} · MIT License" : "MIT License",
            Content = new TextBlock { Text = info.Version, Style = Styles.Get("DimTextStyle") },
        };
        if (info.IconPath is { } icon)
            about.HeaderIcon = new ImageIcon { Source = new BitmapImage(new Uri(icon)) };
        if (Link(T("Website"), info.Website) is { } website)
            about.Items.Add(website);
        if (Link(T("Report an Issue"), info.IssueUrl) is { } issues)
            about.Items.Add(issues);
        if (info.Licenses is { Count: > 0 })
        {
            var licenses = new SettingsCard { Header = T("Included Software"), IsClickEnabled = true };
            licenses.Click += (_, _) => showLicenses();
            about.Items.Add(licenses);
        }
        page.Children.Add(about);

        scroller.Content = page;
    }

    public UIElement Root => scroller;

    /// <summary>To the top; with <paramref name="toAbout"/>, About opened and in view.</summary>
    public void Reveal(bool toAbout)
    {
        if (toAbout)
        {
            about.IsExpanded = true;
            about.StartBringIntoView();
        }
        else
        {
            scroller.ChangeView(null, 0, null, disableAnimation: true);
        }
    }

    private static TextBlock SectionHeader(string title) => new()
    {
        Text = title,
        Style = Styles.Get("BodyStrongTextBlockStyle"),
        Margin = new Thickness(1, 24, 0, 6),
    };

    private static SettingsCard Card(string glyph, string title, string? description, FrameworkElement control)
    {
        var card = new SettingsCard { HeaderIcon = new FontIcon { Glyph = glyph }, Header = title, Content = control };
        if (description is not null)
            card.Description = description;
        return card;
    }

    private static SettingsCard? Link(string title, string? url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return null;
        return new SettingsCard
        {
            Header = title,
            Content = new HyperlinkButton { Content = uri.Host + uri.AbsolutePath.TrimEnd('/'), NavigateUri = uri },
        };
    }

    /// <summary>A new instance first, then this one goes.</summary>
    private static void Relaunch()
    {
        if (Environment.ProcessPath is not { } executable)
            return;
        try
        {
            Process.Start(new ProcessStartInfo(executable) { UseShellExecute = false });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or IOException)
        {
            return;
        }
        Application.Current.Exit();
    }
}
