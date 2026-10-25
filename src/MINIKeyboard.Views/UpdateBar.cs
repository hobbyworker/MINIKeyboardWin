using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using static MINIKeyboard.Views.Strings;

namespace MINIKeyboard.Views;

/// <summary>
/// The app's own updates, in the build that has them — the direct download's. A plain build and the Microsoft
/// Store's pass none, and have no update menu, setting or bar.
/// </summary>
public interface IAppUpdates
{
    /// <summary>A newer version to update to ("1.1.0"), or null when this one is the newest. Throws when it can't look.</summary>
    Task<string?> CheckAsync();

    /// <summary>
    /// Downloads the version <see cref="CheckAsync"/> found, telling how far it got (0–100), then quits the app and
    /// opens that version. Throws when the download fails.
    /// </summary>
    Task UpdateAndRestartAsync(Action<int> progress);
}

/// <summary>
/// A bar across the window for updates: a newer version, with the button that brings it in; and, asked from the
/// menu, that there is none or that the look failed. It looks by itself at launch and once a day while the app runs,
/// unless Settings say not to.
/// </summary>
internal sealed class UpdateBar
{
    private static readonly TimeSpan FirstLook = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan Daily = TimeSpan.FromDays(1);
    private static readonly TimeSpan UpToDateLifetime = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan FailedLifetime = TimeSpan.FromSeconds(6);

    private readonly IAppUpdates updates;
    private readonly InterfaceSettings settings;
    private readonly string appName;
    private readonly DispatcherQueue queue = DispatcherQueue.GetForCurrentThread();
    private readonly InfoBar bar = new()
    {
        IsOpen = false,
        IsClosable = true,
        Margin = new Thickness(16, 8, 16, 0),
        Visibility = Visibility.Collapsed,
    };
    private readonly Button update = new() { Content = T("Update and Relaunch") };
    private readonly ProgressBar progress = new()
    {
        Maximum = 100,
        Margin = new Thickness(0, 0, 0, 14),
        Visibility = Visibility.Collapsed,
    };
    private readonly DispatcherQueueTimer first;
    private readonly DispatcherQueueTimer daily;
    private DispatcherQueueTimer? hide;
    private bool busy;

    public UpdateBar(IAppUpdates updates, InterfaceSettings settings, string appName)
    {
        this.updates = updates;
        this.settings = settings;
        this.appName = appName;
        update.Style = Styles.Get("AccentButtonStyle");
        update.Click += (_, _) => _ = UpdateAsync();
        bar.Content = progress;
        bar.Closed += (_, _) => bar.Visibility = Visibility.Collapsed;

        daily = queue.CreateTimer();
        daily.Interval = Daily;
        daily.Tick += (_, _) => Look();
        first = queue.CreateTimer();
        first.Interval = FirstLook;
        first.IsRepeating = false;
        first.Tick += (_, _) =>
        {
            Look();
            daily.Start();
        };
        first.Start();
    }

    public UIElement Root => bar;

    /// <summary>
    /// Looks for a newer version. Asked from the menu, the bar says how it went either way; by itself, only when
    /// there is one.
    /// </summary>
    public async Task CheckAsync(bool asked)
    {
        if (busy)
            return;
        busy = true;
        string? version;
        try
        {
            version = await updates.CheckAsync();
        }
        // The network, GitHub's limits, a list it can't read: whatever stopped it, the look failed.
        catch (Exception)
        {
            if (asked)
                Show(InfoBarSeverity.Error, T("Couldn’t check for updates."), offersUpdate: false, FailedLifetime);
            return;
        }
        finally
        {
            busy = false;
        }
        if (version is not null)
            Show(InfoBarSeverity.Informational, T("%1$@ %2$@ is available.", appName, version), offersUpdate: true);
        else if (asked)
            Show(InfoBarSeverity.Success, T("%@ is up to date.", appName), offersUpdate: false, UpToDateLifetime);
    }

    /// <summary>Stops looking — the window is going.</summary>
    public void Stop()
    {
        first.Stop();
        daily.Stop();
        hide?.Stop();
    }

    private void Look()
    {
        if (settings.Choices.CheckForUpdates)
            _ = CheckAsync(asked: false);
    }

    /// <summary>Brings in the version the bar offers: the bar shows the download, then the app restarts on it.</summary>
    private async Task UpdateAsync()
    {
        if (busy)
            return;
        busy = true;
        hide?.Stop();
        update.IsEnabled = false;
        bar.IsClosable = false;
        progress.Value = 0;
        progress.Visibility = Visibility.Visible;
        try
        {
            // How far it got comes from the download's own threads.
            await updates.UpdateAndRestartAsync(percent => queue.TryEnqueue(() => progress.Value = percent));
        }
        catch (Exception)
        {
            Show(InfoBarSeverity.Error, T("Couldn’t download the update."), offersUpdate: true);
        }
        finally
        {
            busy = false;
            update.IsEnabled = true;
            bar.IsClosable = true;
        }
    }

    /// <param name="offersUpdate">With the button that brings in the version found.</param>
    /// <param name="lifetime">How long it stays; without one, until it is closed.</param>
    private void Show(InfoBarSeverity severity, string title, bool offersUpdate, TimeSpan? lifetime = null)
    {
        hide?.Stop();
        bar.Severity = severity;
        bar.Title = title;
        bar.ActionButton = offersUpdate ? update : null;
        progress.Visibility = Visibility.Collapsed;
        bar.Visibility = Visibility.Visible;
        bar.IsOpen = true;
        if (lifetime is not { } span)
            return;
        hide = queue.CreateTimer();
        hide.Interval = span;
        hide.IsRepeating = false;
        hide.Tick += (_, _) => bar.IsOpen = false;
        hide.Start();
    }
}
