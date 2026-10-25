using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using static MINIKeyboard.Data.Connection.NativeMethods;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// A configuration collection opened for overlapped I/O: requests go out as output reports (<c>WriteFile</c>), answers
/// come in as input reports (<c>ReadFile</c>) from the queue the HID class driver keeps. Every transfer is waited for
/// on the calling thread — the channel's own — so its buffer and <c>OVERLAPPED</c> stay put until it is over.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsHidPort(
    SafeFileHandle handle, ReportFraming framing, HidCollection collection, bool readsAnswers) : IHidPort
{
    /// <summary>The Windows tool's write timeout (<c>HidLib.WriteDevice</c>).</summary>
    private static readonly TimeSpan WriteTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>Answers are 64-byte reports; shorter ones are the keyboard's typing, media keys or mouse.</summary>
    private const int MinimumReplyLength = 32;

    /// <summary>A USB string holds at most 126 characters; the API adds the terminating null.</summary>
    private const int StringCharacters = 127;

    /// <summary>Signalled when a transfer finishes; the transfer itself resets it as it starts.</summary>
    private readonly ManualResetEvent finished = new(false);

    public int Write(byte[] report)
    {
        var buffer = HidCollections.OutputBuffer(report, framing, collection.OutputReportByteLength);
        fixed (byte* pointer = buffer)
        {
            var overlapped = new NativeOverlapped { EventHandle = finished.SafeWaitHandle.DangerousGetHandle() };
            var timedOut = false;
            if (!WriteFile(handle, pointer, (uint)buffer.Length, null, &overlapped))
            {
                var error = Marshal.GetLastPInvokeError();
                if (error != ErrorIoPending)
                    return error;
                if (!finished.WaitOne(WriteTimeout))
                {
                    timedOut = true;
                    CancelIoEx(handle, &overlapped);
                }
            }
            // Waits for the end — at once, or once cancelled — before the buffer is let go.
            if (!GetOverlappedResult(handle, &overlapped, out var written, wait: true))
            {
                var error = Marshal.GetLastPInvokeError();
                return timedOut ? ErrorTimeout : error;
            }
            return written == buffer.Length ? 0 : ErrorWriteFault;
        }
    }

    /// <summary>
    /// The next input report of an answer's length, its report ID first; shorter ones are passed over.
    /// </summary>
    public byte[]? Read(TimeSpan timeout)
    {
        if (!readsAnswers)
            return null;
        var deadline = Environment.TickCount64 + (long)timeout.TotalMilliseconds;
        var buffer = new byte[collection.InputReportByteLength];
        while (true)
        {
            var left = Math.Clamp(deadline - Environment.TickCount64, 0, int.MaxValue);
            if (ReadOnce(buffer, (int)left) is not { } length)
                return null;
            if (length >= MinimumReplyLength)
                return buffer[..length];
        }
    }

    /// <summary>Drops the input reports the driver has queued.</summary>
    public void Discard()
    {
        if (readsAnswers)
            HidD_FlushQueue(handle);
    }

    /// <summary>
    /// The device's strings, as the HID driver reads them from its string descriptors — standard read requests, made
    /// only for strings the device declares.
    /// </summary>
    public DeviceStrings Strings() => new(
        Text(&HidD_GetManufacturerString), Text(&HidD_GetProductString), Text(&HidD_GetSerialNumberString));

    public void Dispose()
    {
        handle.Dispose();
        finished.Dispose();
    }

    /// <summary>
    /// One input report, or null when none comes within <paramref name="milliseconds"/> or reading fails.
    /// </summary>
    private int? ReadOnce(byte[] buffer, int milliseconds)
    {
        fixed (byte* pointer = buffer)
        {
            var overlapped = new NativeOverlapped { EventHandle = finished.SafeWaitHandle.DangerousGetHandle() };
            if (!ReadFile(handle, pointer, (uint)buffer.Length, null, &overlapped))
            {
                if (Marshal.GetLastPInvokeError() != ErrorIoPending)
                    return null;
                // A report that comes as the read is cancelled is still read; one not yet taken stays queued.
                if (!finished.WaitOne(milliseconds))
                    CancelIoEx(handle, &overlapped);
            }
            if (!GetOverlappedResult(handle, &overlapped, out var read, wait: true))
                return null;
            return (int)read;
        }
    }

    private string? Text(delegate*<SafeFileHandle, char*, uint, bool> read)
    {
        var buffer = stackalloc char[StringCharacters];
        new Span<char>(buffer, StringCharacters).Clear();
        if (!read(handle, buffer, StringCharacters * sizeof(char)))
            return null;
        var characters = new ReadOnlySpan<char>(buffer, StringCharacters);
        var end = characters.IndexOf('\0');
        var text = new string(end < 0 ? characters : characters[..end]);
        return text.Length == 0 ? null : text;
    }
}
