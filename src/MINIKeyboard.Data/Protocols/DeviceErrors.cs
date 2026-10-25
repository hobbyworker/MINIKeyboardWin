using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Protocols;

internal static class DeviceErrors
{
    /// <summary>The keyboard has no command for this.</summary>
    public static KeyboardDeviceException Unsupported() => new(new KeyboardDeviceError.Unsupported());
}
