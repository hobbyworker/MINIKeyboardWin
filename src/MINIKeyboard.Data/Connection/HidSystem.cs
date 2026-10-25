using System.Diagnostics.CodeAnalysis;

namespace MINIKeyboard.Data.Connection;

/// <summary>The system's HID devices, as the connection looks at and opens them.</summary>
internal interface IHidSystem
{
    /// <summary>Every top-level collection of a supported keyboard plugged in.</summary>
    IReadOnlyList<HidCollection> Collections();

    /// <summary>
    /// Opens <paramref name="collection"/> to write its output reports and, when <paramref name="readsAnswers"/>, read
    /// its input reports. <paramref name="error"/> is the Windows error code when it doesn't open.
    /// </summary>
    bool TryOpen(
        HidCollection collection, ReportFraming framing, bool readsAnswers, [NotNullWhen(true)] out IHidPort? port,
        out int error);
}

/// <summary>
/// The open collection a channel writes through. Calls come from one thread at a time, and every one returns once the
/// transfer is over. Error values are Windows error codes, as the log shows them.
/// </summary>
internal interface IHidPort : IDisposable
{
    /// <summary>Writes one report, as framed: 0 when it went out, otherwise the error code.</summary>
    int Write(byte[] report);

    /// <summary>The next answer of at least the length answers have, or null when none comes in time.</summary>
    byte[]? Read(TimeSpan timeout);

    /// <summary>Drops what came before.</summary>
    void Discard();

    DeviceStrings Strings();
}
