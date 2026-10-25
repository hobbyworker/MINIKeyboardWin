using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using MINIKeyboard.Domain;
using static MINIKeyboard.Data.Connection.NativeMethods;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// The HID devices Windows lists, as the vendor tools find theirs: every HID device interface present, its USB IDs
/// from <c>HidD_GetAttributes</c>, its interface from <c>mi_XX</c> in its path, and what its parsed report descriptor
/// says (<c>HidP_GetCaps</c>). Both families' configuration interfaces are plain HID devices on Windows — the HID class
/// driver takes the 1189:8890's interface 1 too, as the vendor's Windows tool relies on — so nothing needs installing
/// or allowing.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed unsafe class WindowsHid : IHidSystem
{
    public IReadOnlyList<HidCollection> Collections()
    {
        HidD_GetHidGuid(out var hidGuid);
        var set = SetupDiGetClassDevsW(hidGuid, 0, 0, DigcfPresent | DigcfDeviceInterface);
        if (set == InvalidHandle)
            return [];
        var found = new List<HidCollection>();
        try
        {
            for (uint index = 0; ; index++)
            {
                var data = new SpDeviceInterfaceData { Size = (uint)sizeof(SpDeviceInterfaceData) };
                // Fails with ERROR_NO_MORE_ITEMS after the last one.
                if (!SetupDiEnumDeviceInterfaces(set, 0, hidGuid, index, ref data))
                    break;
                if (Detail(set, ref data) is { } detail && Describe(detail.Path, detail.DevInst) is { } collection)
                    found.Add(collection);
            }
        }
        finally
        {
            SetupDiDestroyDeviceInfoList(set);
        }
        return found;
    }

    public bool TryOpen(
        HidCollection collection, ReportFraming framing, bool readsAnswers, [NotNullWhen(true)] out IHidPort? port,
        out int error)
    {
        // Shared, as the vendor's Windows tool opens it: another app may have the keyboard open too.
        var handle = CreateFile(
            collection.Path, GenericRead | GenericWrite, FileShareRead | FileShareWrite, 0, OpenExisting,
            FileFlagOverlapped, 0);
        if (handle.IsInvalid)
        {
            error = Marshal.GetLastPInvokeError();
            handle.Dispose();
            port = null;
            return false;
        }
        port = new WindowsHidPort(handle, framing, collection, readsAnswers);
        error = 0;
        return true;
    }

    /// <summary>The device interface's path, and the device node it belongs to.</summary>
    private static (string Path, uint DevInst)? Detail(nint set, ref SpDeviceInterfaceData data)
    {
        // The first call only says how large the detail is.
        SetupDiGetDeviceInterfaceDetailW(set, ref data, null, 0, out var required, null);
        if (required <= sizeof(uint))
            return null;
        var buffer = new byte[required];
        // SP_DEVICE_INTERFACE_DETAIL_DATA_W.cbSize: the structure's own size — 8 on 64-bit Windows, 6 on 32-bit.
        BinaryPrimitives.WriteUInt32LittleEndian(buffer, IntPtr.Size == 8 ? 8u : 6u);
        var device = new SpDevinfoData { Size = (uint)sizeof(SpDevinfoData) };
        fixed (byte* detail = buffer)
        {
            if (!SetupDiGetDeviceInterfaceDetailW(set, ref data, detail, required, out _, &device))
                return null;
            var characters = new ReadOnlySpan<char>(detail + sizeof(uint), (int)(required - sizeof(uint)) / 2);
            var end = characters.IndexOf('\0');
            return (new string(end < 0 ? characters : characters[..end]), device.DevInst);
        }
    }

    /// <summary>
    /// What the HID API says of one collection of a supported keyboard; null for any other device. The device is
    /// opened with no access — enough for its attributes and caps, even while the system has it open, and nothing is
    /// sent to it.
    /// </summary>
    private static HidCollection? Describe(string path, uint devInst)
    {
        using var handle = CreateFile(path, 0, FileShareRead | FileShareWrite, 0, OpenExisting, 0, 0);
        if (handle.IsInvalid)
            return null;
        var attributes = new HiddAttributes { Size = (uint)sizeof(HiddAttributes) };
        if (!HidD_GetAttributes(handle, ref attributes))
            return null;
        var usb = new UsbIdentity(attributes.VendorId, attributes.ProductId);
        if (KeyboardFamilies.Of(usb) is null)
            return null;
        if (!HidD_GetPreparsedData(handle, out var preparsed))
            return null;
        try
        {
            if (HidP_GetCaps(preparsed, out var caps) != HidpStatusSuccess)
                return null;
            var number = HidCollections.InterfaceNumber(path);
            return new HidCollection(
                path,
                DeviceKey(devInst, composite: number is not null) ?? path,
                usb,
                attributes.VersionNumber,
                // A device that isn't composite has one interface: interface 0.
                number ?? 0,
                caps.UsagePage,
                caps.Usage,
                caps.InputReportByteLength,
                caps.OutputReportByteLength,
                OutputReportId(preparsed, caps));
        }
        finally
        {
            HidD_FreePreparsedData(preparsed);
        }
    }

    /// <summary>
    /// The report ID of the first output value, or else button, the parsed descriptor lists: 0 when the reports aren't
    /// numbered, null when it lists none.
    /// </summary>
    private static byte? OutputReportId(nint preparsed, in HidpCaps caps)
    {
        if (caps.NumberOutputValueCaps > 0)
        {
            var length = caps.NumberOutputValueCaps;
            var values = new HidpReportCaps[length];
            fixed (HidpReportCaps* pointer = values)
            {
                if (HidP_GetValueCaps(HidpOutput, pointer, ref length, preparsed) == HidpStatusSuccess && length > 0)
                    return values[0].ReportId;
            }
        }
        if (caps.NumberOutputButtonCaps > 0)
        {
            var length = caps.NumberOutputButtonCaps;
            var buttons = new HidpReportCaps[length];
            fixed (HidpReportCaps* pointer = buttons)
            {
                if (HidP_GetButtonCaps(HidpOutput, pointer, ref length, preparsed) == HidpStatusSuccess && length > 0)
                    return buttons[0].ReportId;
            }
        }
        return null;
    }

    /// <summary>
    /// The device instance ID of the USB device a collection belongs to. A collection's parent is its USB interface on
    /// a composite device — whose parent is the device — and the device itself otherwise.
    /// </summary>
    private static string? DeviceKey(uint devInst, bool composite)
    {
        if (CM_Get_Parent(out var device, devInst, 0) != CrSuccess)
            return null;
        if (composite && CM_Get_Parent(out device, device, 0) != CrSuccess)
            return null;
        const int length = MaxDeviceIdLength + 1;
        var buffer = stackalloc char[length];
        new Span<char>(buffer, length).Clear();
        if (CM_Get_Device_IDW(device, buffer, length, 0) != CrSuccess)
            return null;
        var characters = new ReadOnlySpan<char>(buffer, length);
        var end = characters.IndexOf('\0');
        return new string(end < 0 ? characters : characters[..end]);
    }
}
