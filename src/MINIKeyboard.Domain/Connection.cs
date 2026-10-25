namespace MINIKeyboard.Domain;

/// <summary>How the app reaches a keyboard's configuration interface.</summary>
public enum Channel
{
    /// <summary>Written and read as a HID device — on Windows, both families' configuration interfaces.</summary>
    Hid,

    /// <summary>A keyboard that exists only in the app.</summary>
    Simulated,
}

/// <summary>A keyboard whose configuration interface the app has open.</summary>
/// <param name="ReportId">The report ID the configuration goes after, from the interface's report descriptor.</param>
/// <param name="ReportLength">Bytes per output report, the report ID included.</param>
/// <param name="Details">What the USB device says about itself; null for a simulated keyboard.</param>
public sealed record ConnectedKeyboard(
    UsbIdentity Usb,
    KeyboardFamily Family,
    byte InterfaceNumber,
    Channel Channel,
    byte ReportId,
    int ReportLength,
    DeviceDetails? Details);

public abstract record ConnectionState
{
    private ConnectionState() { }

    public static ConnectionState Searching { get; } = new SearchingState();

    public sealed record SearchingState : ConnectionState;

    public sealed record Connected(ConnectedKeyboard Device) : ConnectionState;

    /// <summary>The system lets the app open the keyboard only once it is allowed to.</summary>
    public sealed record NeedsPermission : ConnectionState;

    /// <summary>Opening failed for another reason (a Windows error code).</summary>
    public sealed record OpenFailed(int Code) : ConnectionState;

    public ConnectedKeyboard? Keyboard => this is Connected connected ? connected.Device : null;
}

/// <summary>Why a transfer with the keyboard failed.</summary>
public abstract record KeyboardDeviceError
{
    private KeyboardDeviceError() { }

    /// <summary>No keyboard is open.</summary>
    public sealed record NotConnected : KeyboardDeviceError
    {
        public override string ToString() => "no keyboard is open";
    }

    /// <summary>A report could not be written (a Windows error code).</summary>
    public sealed record WriteFailed(int Code) : KeyboardDeviceError
    {
        public override string ToString() => $"a report could not be written ({Code})";
    }

    /// <summary>An answer did not come in time.</summary>
    public sealed record NoReply : KeyboardDeviceError
    {
        public override string ToString() => "no answer came in time";
    }

    /// <summary>An answer was too short to read.</summary>
    public sealed record InvalidReply : KeyboardDeviceError
    {
        public override string ToString() => "an answer was too short";
    }

    /// <summary>The keyboard has no command for this.</summary>
    public sealed record Unsupported : KeyboardDeviceError
    {
        public override string ToString() => "the keyboard has no command for this";
    }
}

/// <summary>A transfer with the keyboard failed; <see cref="Error"/> says why.</summary>
public sealed class KeyboardDeviceException(KeyboardDeviceError error) : Exception(error.ToString())
{
    public KeyboardDeviceError Error { get; } = error;
}

/// <summary>
/// What the USB device and its interfaces say about themselves — read from what the system already knows, never asked
/// of the keyboard. These keyboards carry no strings and their vendor IDs aren't the maker's, so nothing here is
/// guessed at.
/// </summary>
/// <param name="UsbVersion"><c>bcdUSB</c>, e.g. 0x0110; null where the system doesn't tell (the HID API doesn't).</param>
/// <param name="DeviceRelease"><c>bcdDevice</c>; 0 on keyboards that don't say.</param>
/// <param name="Speed">Link speed in bits per second.</param>
/// <param name="MaxPower">The most current the keyboard asks for, in mA (<c>bMaxPower</c>).</param>
public sealed record DeviceDetails(
    ushort? UsbVersion,
    ushort DeviceRelease,
    ulong? Speed,
    string? Manufacturer,
    string? ProductName,
    string? SerialNumber,
    uint? MaxPower,
    EquatableArray<DeviceInterface> Interfaces);

/// <param name="Subclass">Null where the system doesn't tell (the HID API doesn't).</param>
/// <param name="Protocol">Null where the system doesn't tell (the HID API doesn't).</param>
/// <param name="Usages">
/// The (usage page, usage) of each top-level collection of the interface's HID report descriptor; empty when the
/// system made no HID device of it.
/// </param>
public sealed record DeviceInterface(byte Number, byte Class, byte? Subclass, byte? Protocol, EquatableArray<UsagePair> Usages);

public readonly record struct UsagePair(ushort Page, ushort Usage);
