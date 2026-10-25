#if DEBUG
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;
using MINIKeyboard.Presentation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace MINIKeyboard.Views;

/// <summary>
/// Looking at the windows without a screen in front of you (debug builds). With <c>MINIKEYBOARD_SCREENSHOT=&lt;file.png&gt;</c>
/// the app takes the steps in <c>MINIKEYBOARD_SCREENSHOT_STEPS</c>, waits for them to settle
/// (<c>MINIKEYBOARD_SCREENSHOT_DELAY</c>, in milliseconds), draws a window's client area — dialogs and flyouts included
/// — into the file, and quits: the main window, or with <c>MINIKEYBOARD_SCREENSHOT_WINDOW=log</c> the device log
/// (<c>popover</c>, the keyboard information, is drawn inside the main window).
/// </summary>
/// <remarks>
/// The steps, separated by <c>;</c>, are what a user would do: <c>arrangement:6,3,1</c>, <c>layout:9add2</c>,
/// <c>select:key:3</c>, <c>select:knob:1:press</c>, <c>modifier:super</c>, <c>press:0x06</c>, <c>tab:media</c>,
/// <c>keys:qwerty</c>, <c>media:8</c>, <c>mouse:3</c>, <c>mode:2</c>, <c>color:255,0,0</c>, <c>layer:2</c>,
/// <c>info</c>, and <c>action:&lt;name&gt;</c> for any of the window's commands (<c>device-log</c>,
/// <c>preferences</c>, <c>about</c>, <c>shape</c> …); and, to look at what the other editions' steps don't show,
/// <c>appearance:dark</c> (or <c>light</c>, <c>system</c>) and <c>menu</c>, the main menu.
/// </remarks>
public static partial class Screenshot
{
    public static string? RequestedPath => Environment.GetEnvironmentVariable("MINIKEYBOARD_SCREENSHOT");

    public static TimeSpan Delay =>
        TimeSpan.FromMilliseconds(int.TryParse(Environment.GetEnvironmentVariable("MINIKEYBOARD_SCREENSHOT_DELAY"),
            out var milliseconds) ? milliseconds : 1500);

    /// <summary>Takes the steps, draws the window asked for into <paramref name="path"/>, then calls
    /// <paramref name="done"/>.</summary>
    public static async Task RunAsync(MainWindow main, string path, Action done)
    {
        // A display the system turned off for being idle draws nothing — keep it on while the window is drawn.
        SetThreadExecutionState(EsContinuous | EsDisplayRequired);
        var steps = Environment.GetEnvironmentVariable("MINIKEYBOARD_SCREENSHOT_STEPS") ?? "";
        var target = Environment.GetEnvironmentVariable("MINIKEYBOARD_SCREENSHOT_WINDOW") ?? "";
        // The keyboard connects and is identified — and read, when it reads back — first.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);
        await Task.Delay(400);
        while (!main.Editor.IsReady && DateTime.UtcNow < deadline)
            await Task.Delay(50);
        await Task.Delay(200);
        foreach (var step in steps.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            if (!Take(main, step))
                System.Diagnostics.Debug.WriteLine($"Unknown screenshot step: {step}");
            await Task.Delay(50);
        }
        await Task.Delay(Delay);
        Window window = target == "log" && main.LogWindow is { } log ? log : main;
        await SaveAsync(window, path);
        done();
    }

    private static bool Take(MainWindow main, string step)
    {
        var editor = main.Editor;
        var colon = step.IndexOf(':');
        var (verb, argument) = colon < 0 ? (step, "") : (step[..colon], step[(colon + 1)..]);
        byte[] Numbers() => [.. argument.Split(',').Select(part =>
            byte.TryParse(part.Trim(), CultureInfo.InvariantCulture, out var value) ? (byte?)value : null)
            .OfType<byte>()];
        switch (verb)
        {
            case "arrangement" when Numbers() is [var keys, var columns, var knobs]:
                editor.ChooseArrangement(keys, columns, knobs, 0);
                return true;
            case "layout":
                editor.ChooseLayout(argument, 0);
                return true;
            case "select":
                var parts = argument.Split(':');
                ControlRef? control = parts switch
                {
                    ["key", var number] when byte.TryParse(number, CultureInfo.InvariantCulture, out var key) =>
                        ControlRef.Key(key),
                    ["knob", var number, var action]
                        when byte.TryParse(number, CultureInfo.InvariantCulture, out var knob)
                            && KnobActionNamed(action) is { } part => ControlRef.Knob(knob, part),
                    _ => null,
                };
                if (control is not { } selected)
                    return false;
                editor.Select(selected);
                return true;
            case "modifier":
                // By its label ("Ctrl", "Win") or its name ("super").
                var modifier = ModifierKeys.All.Cast<ModifierKey?>().FirstOrDefault(each =>
                    string.Equals(each!.Value.Label(), argument, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(each.Value.ToString(), argument, StringComparison.OrdinalIgnoreCase));
                if (modifier is not { } held)
                    return false;
                editor.Toggle(held);
                return true;
            case "press":
                var hex = argument.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? argument[2..] : argument;
                if (!byte.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var usage)
                    || KeyUsage.Named(usage) is not { } pressed)
                    return false;
                editor.Press(pressed);
                return true;
            case "tab":
                EditorTab? tab = argument switch
                {
                    "keys" => EditorTab.Keys,
                    "media" => EditorTab.Media,
                    "mouse" => EditorTab.Mouse,
                    "lighting" => EditorTab.Lighting,
                    "delay" => EditorTab.Delay,
                    _ => null,
                };
                if (tab is not { } shown)
                    return false;
                editor.SetTab(shown);
                return true;
            case "keys":
                main.Settings.Update(choices => choices with { KeyArrangement = argument });
                editor.SetTab(EditorTab.Keys);
                return true;
            case "media" when int.TryParse(argument, CultureInfo.InvariantCulture, out var index)
                && index >= 0 && index < MediaChoices.All.Count:
                editor.AssignMedia(MediaChoices.All[index]);
                return true;
            case "mouse" when int.TryParse(argument, CultureInfo.InvariantCulture, out var index)
                && index >= 0 && index < MouseChoices.All.Count:
                editor.AssignMouse(MouseChoices.All[index]);
                return true;
            case "mode" when Numbers() is [var mode]:
                editor.SetLightingMode(mode);
                return true;
            case "color" when Numbers() is [var red, var green, var blue]:
                editor.SetKeyColor(new Rgb(red, green, blue));
                return true;
            case "layer" when Numbers() is [var layer] && layer > 0:
                editor.SetLayer(layer - 1);
                return true;
            case "info":
                main.ShowKeyboardInfo();
                return true;
            case "appearance":
                Appearance? appearance = argument switch
                {
                    "system" => Appearance.System,
                    "light" => Appearance.Light,
                    "dark" => Appearance.Dark,
                    _ => null,
                };
                if (appearance is not { } chosen)
                    return false;
                main.Settings.Update(choices => choices with { Appearance = chosen });
                main.ApplyAppearance(chosen);
                return true;
            case "menu":
                // Drawn inside the window, where the capture sees it.
                main.Header.Menu.ShouldConstrainToRootBounds = true;
                main.Header.Menu.ShowAt(main.Header.More);
                return true;
            case "action":
                return main.Run(argument);
            default:
                return false;
        }
    }

    private static KnobAction? KnobActionNamed(string name) => name switch
    {
        "left" => KnobAction.RotateLeft,
        "press" => KnobAction.Press,
        "right" => KnobAction.RotateRight,
        "press-left" => KnobAction.PressRotateLeft,
        "press-right" => KnobAction.PressRotateRight,
        _ => null,
    };

    /// <summary>Draws <paramref name="window"/>'s client area into a PNG at <paramref name="path"/>.</summary>
    public static async Task SaveAsync(Window window, string path)
    {
        var handle = WindowNative.GetWindowHandle(window);
        var (width, height, pixels) = Capture(handle);
        using var memory = new InMemoryRandomAccessStream();
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, memory);
        encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore, (uint)width, (uint)height, 96, 96, pixels);
        await encoder.FlushAsync();
        var bytes = new byte[memory.Size];
        using (var reader = new DataReader(memory.GetInputStreamAt(0)))
        {
            await reader.LoadAsync((uint)memory.Size);
            reader.ReadBytes(bytes);
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        await File.WriteAllBytesAsync(path, bytes);
    }

    /// <summary>The client area as top-down BGRA rows, drawn by the window itself (<c>PrintWindow</c>).</summary>
    private static unsafe (int Width, int Height, byte[] Pixels) Capture(nint window)
    {
        GetClientRect(window, out var client);
        var width = client.Right - client.Left;
        var height = client.Bottom - client.Top;
        var screen = GetDC(0);
        var memory = CreateCompatibleDC(screen);
        var info = new BitmapInfoHeader
        {
            Size = (uint)sizeof(BitmapInfoHeader),
            Width = width,
            Height = -height,
            Planes = 1,
            BitCount = 32,
        };
        var bitmap = CreateDIBSection(memory, &info, 0, out var bits, 0, 0);
        var previous = SelectObject(memory, bitmap);
        try
        {
            // PW_CLIENTONLY | PW_RENDERFULLCONTENT: the client area, with what the compositor draws.
            PrintWindow(window, memory, 0x1 | 0x2);
            var pixels = new byte[width * height * 4];
            Marshal.Copy(bits, pixels, 0, pixels.Length);
            return (width, height, pixels);
        }
        finally
        {
            SelectObject(memory, previous);
            DeleteObject(bitmap);
            DeleteDC(memory);
            _ = ReleaseDC(0, screen);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BitmapInfoHeader
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant;
    }

    private const uint EsContinuous = 0x80000000;
    private const uint EsDisplayRequired = 0x00000002;

    [LibraryImport("kernel32.dll")]
    private static partial uint SetThreadExecutionState(uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetClientRect(nint window, out Rect rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool PrintWindow(nint window, nint deviceContext, uint flags);

    [LibraryImport("user32.dll")]
    private static partial nint GetDC(nint window);

    [LibraryImport("user32.dll")]
    private static partial int ReleaseDC(nint window, nint deviceContext);

    [LibraryImport("gdi32.dll")]
    private static partial nint CreateCompatibleDC(nint deviceContext);

    [LibraryImport("gdi32.dll")]
    private static unsafe partial nint CreateDIBSection(
        nint deviceContext, BitmapInfoHeader* info, uint usage, out nint bits, nint section, uint offset);

    [LibraryImport("gdi32.dll")]
    private static partial nint SelectObject(nint deviceContext, nint gdiObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteObject(nint gdiObject);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool DeleteDC(nint deviceContext);
}
#endif
