using System.Reflection;
using Microsoft.UI.Xaml;
using MINIKeyboard.Data;
using MINIKeyboard.Domain;
using MINIKeyboard.Domain.UseCases;
using MINIKeyboard.Presentation;
using MINIKeyboard.Views;

namespace MINIKeyboard;

/// <summary>
/// The composition root: the one place that knows the concrete repositories, and puts the use cases, the view models
/// and the windows together. Everything else depends on the domain's ports or the presentation's types.
/// </summary>
public partial class App : Application
{
    private static readonly Assembly Self = typeof(App).Assembly;

    /// <summary>The app's name — the brand's (Brand.props), which the build puts in the assembly as its product.</summary>
    private static readonly string Name =
        Self.GetCustomAttribute<AssemblyProductAttribute>()?.Product ?? Self.GetName().Name ?? "";

    private readonly CancellationTokenSource running = new();
    private MainWindow? window;
    private IDisposable? connection;

    public App() => InitializeComponent();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Resources.MergedDictionaries.Add(new Styles());
        var data = DataFolder();
        var settings = InterfaceSettings.Load(Path.Combine(data, "settings.json"));
        // The language is read once, at launch; the environment's wins, for trying a language.
        var language = Environment.GetEnvironmentVariable("MINIKEYBOARD_LANGUAGE");
        Strings.Initialize(string.IsNullOrEmpty(language) ? settings.Choices.Language : language);
        // A language chosen in the app also goes to the words Windows draws itself (a switch's On and Off …).
        if (Strings.Language is not null)
            Microsoft.Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = Strings.Current;

        var log = new DeviceLogStore();
        var profiles = new FileLayoutProfiles(Path.Combine(data, "layouts.json"));
        // Settings ▸ Keyboard, asked at every write.
        var (keyboards, device, source, disposable) =
            Connect(log, () => settings.Choices.PreventLightingKeyRepeat);
        connection = disposable;
        var editor = new EditorModel(new EditorDependencies
        {
            ObserveConnection = new ObserveConnectionUseCase(keyboards),
            RetryConnection = new RetryConnectionUseCase(keyboards),
            IdentifyKeyboard = new IdentifyKeyboardUseCase(device),
            ResolveLayout = new ResolveLayoutUseCase(profiles),
            ChooseLayout = new ChooseLayoutUseCase(profiles),
            ReadKeyboard = new ReadKeyboardUseCase(device),
            SaveKeyboard = new SaveKeyboardUseCase(device),
            KeyboardSource = source,
        });
        var deviceLog = new DeviceLogModel(new DeviceLogUseCase(log));

        window = new MainWindow(Info(), settings, editor, deviceLog, Updates());
        // The taskbar's and Alt+Tab's icon; the exe's own is the same file (ApplicationIcon). No brand icon, Windows'.
        if (Asset("AppIcon.ico") is { } icon)
            window.AppWindow.SetIcon(icon);
        window.Closed += (_, _) => Shutdown();
        window.Activate();
        // The view models live on this thread; their work resumes here.
        _ = editor.RunAsync(running.Token);
        _ = deviceLog.RunAsync(running.Token);
#if DEBUG
        if (Screenshot.RequestedPath is { } path)
            _ = Screenshot.RunAsync(window, path, () => window.Close());
#endif
    }

    private void Shutdown()
    {
        running.Cancel();
        connection?.Dispose();
        Exit();
    }

    /// <summary>The app's name, version, copyright, links and icon — the brand's, from the assembly and next to the exe.</summary>
    private static AppInfo Info()
    {
        var version = Self.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Self.GetName().Version?.ToString(3)
            ?? "";
        // "1.0.0+<commit>" names the build; the version is what comes before.
        version = version.Split('+')[0];
        var copyright = Self.GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright;
        return new AppInfo(
            Name, version, copyright, Metadata("Website"), Metadata("IssueUrl"), Asset("AppIcon.png"), Licenses());
    }

    /// <summary>
    /// The licenses of the software the build carries, from Licenses\*.txt next to the exe — each file's first line is
    /// the name, the second where it comes from, the rest its license text. By name; none when there's no such folder.
    /// </summary>
    private static List<LicenseNotice> Licenses()
    {
        var notices = new List<LicenseNotice>();
        try
        {
            foreach (var file in Directory.EnumerateFiles(Path.Combine(AppContext.BaseDirectory, "Licenses"), "*.txt"))
            {
                var parts = File.ReadAllText(file).Split('\n', 3);
                if (parts.Length == 3)
                    notices.Add(new LicenseNotice(parts[0].Trim(), parts[1].Trim(), parts[2].Trim()));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
        notices.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return notices;
    }

    /// <summary>The app's own updates: only the direct download has them (the Store updates its build itself).</summary>
    private static IAppUpdates? Updates() =>
#if DISTRIBUTION_DIRECT
        Direct.VelopackUpdates.Create(Metadata("UpdateUrl"));
#else
        null;
#endif

    /// <summary>A value the build put in the assembly (<c>AssemblyMetadata</c>), or null.</summary>
    private static string? Metadata(string key) =>
        Self.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(item => item.Key == key)?.Value;

    /// <summary>A file in Assets next to the exe, or null when the build has none (no brand icon).</summary>
    private static string? Asset(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", name) is var path && File.Exists(path) ? path : null;

    /// <summary>
    /// Where the app keeps the layouts users chose and its own settings: <c>%LOCALAPPDATA%\MINIKeyboard</c>. Debug
    /// builds take <c>MINIKEYBOARD_DATA_DIR</c> instead, so a try never touches the user's choices.
    /// </summary>
    private static string DataFolder()
    {
#if DEBUG
        if (Environment.GetEnvironmentVariable("MINIKEYBOARD_DATA_DIR") is { Length: > 0 } custom)
            return custom;
#endif
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name);
    }

    /// <summary>
    /// The keyboard on USB or a simulated one, switched from the Simulator menu. A debug build starts on a simulated
    /// keyboard when <c>MINIKEYBOARD_SIMULATED_KEYBOARD</c> names one (a simulated model's ID, e.g. <c>8890-3</c>).
    /// </summary>
    private static (IKeyboardConnectionRepository, IKeyboardDeviceRepository, KeyboardSourceUseCase?, IDisposable)
        Connect(DeviceLogStore log, Func<bool> preventsLightingKeyRepeat)
    {
        KeyboardSource initial = KeyboardSource.Hardware;
#if DEBUG
        var id = Environment.GetEnvironmentVariable("MINIKEYBOARD_SIMULATED_KEYBOARD");
        if (SimulatedModel.All().FirstOrDefault(model => model.Id == id) is { } model)
            initial = new KeyboardSource.Simulated(model);
#endif
        var source = new KeyboardSourceSwitch(log, initial);
        var device = new KeyboardDevice(source, log) { PreventsLightingKeyRepeat = preventsLightingKeyRepeat };
        return (source, device, new KeyboardSourceUseCase(source), source);
    }
}
