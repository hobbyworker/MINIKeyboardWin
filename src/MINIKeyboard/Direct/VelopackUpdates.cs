using MINIKeyboard.Views;
using Velopack;
using Velopack.Sources;

namespace MINIKeyboard.Direct;

/// <summary>
/// Updates through Velopack, from where the brand says (<c>BrandUpdateUrl</c>), on the installed build's own channel
/// (win-x64 or win-arm64): a GitHub repository's releases — the newest that isn't a pre-release — or any other web
/// address or folder that holds Velopack's releases.
/// </summary>
internal sealed class VelopackUpdates : IAppUpdates
{
    private readonly UpdateManager manager;
    private UpdateInfo? found;

    private VelopackUpdates(UpdateManager manager) => this.manager = manager;

    /// <summary>The updates of the app Setup installed; none for one run from where it was built.</summary>
    public static VelopackUpdates? Create(string? feed)
    {
        if (string.IsNullOrEmpty(feed))
            return null;
        var manager = Uri.TryCreate(feed, UriKind.Absolute, out var uri) && uri.Host == "github.com"
            ? new UpdateManager(new GithubSource(feed, accessToken: null, prerelease: false))
            : new UpdateManager(feed);
        return manager.IsInstalled ? new VelopackUpdates(manager) : null;
    }

    public async Task<string?> CheckAsync()
    {
        found = await manager.CheckForUpdatesAsync();
        return found?.TargetFullRelease.Version.ToString();
    }

    public async Task UpdateAndRestartAsync(Action<int> progress)
    {
        if (found is not { } update)
            return;
        await manager.DownloadUpdatesAsync(update, progress);
        manager.ApplyUpdatesAndRestart(update);
    }
}
