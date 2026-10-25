using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace MINIKeyboard.Data.Connection;

// The Windows HID API (hid.dll), the device lists (setupapi.dll, cfgmgr32.dll) and file I/O (kernel32.dll) — only what
// the connection uses. Structures declare the fields read; their sizes are those of the Windows SDK headers.

[SupportedOSPlatform("windows")]
internal static unsafe partial class NativeMethods
{
    public const uint DigcfPresent = 0x02;
    public const uint DigcfDeviceInterface = 0x10;
    public const uint GenericRead = 0x8000_0000;
    public const uint GenericWrite = 0x4000_0000;
    public const uint FileShareRead = 0x1;
    public const uint FileShareWrite = 0x2;
    public const uint OpenExisting = 3;
    public const uint FileFlagOverlapped = 0x4000_0000;

    public const int ErrorWriteFault = 29;
    public const int ErrorIoPending = 997;
    public const int ErrorTimeout = 1460;

    /// <summary><c>HIDP_STATUS_SUCCESS</c>.</summary>
    public const int HidpStatusSuccess = 0x0011_0000;

    /// <summary><c>HidP_Output</c> of <c>HIDP_REPORT_TYPE</c>.</summary>
    public const int HidpOutput = 1;

    /// <summary><c>CR_SUCCESS</c>.</summary>
    public const uint CrSuccess = 0;

    /// <summary><c>MAX_DEVICE_ID_LEN</c>, in characters.</summary>
    public const int MaxDeviceIdLength = 200;

    public static readonly nint InvalidHandle = -1;

    // hid.dll

    [LibraryImport("hid.dll")]
    public static partial void HidD_GetHidGuid(out Guid hidGuid);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetAttributes(SafeFileHandle device, ref HiddAttributes attributes);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetPreparsedData(SafeFileHandle device, out nint preparsedData);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_FreePreparsedData(nint preparsedData);

    [LibraryImport("hid.dll")]
    public static partial int HidP_GetCaps(nint preparsedData, out HidpCaps capabilities);

    [LibraryImport("hid.dll")]
    public static partial int HidP_GetValueCaps(
        int reportType, HidpReportCaps* valueCaps, ref ushort valueCapsLength, nint preparsedData);

    [LibraryImport("hid.dll")]
    public static partial int HidP_GetButtonCaps(
        int reportType, HidpReportCaps* buttonCaps, ref ushort buttonCapsLength, nint preparsedData);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetManufacturerString(SafeFileHandle device, char* buffer, uint bufferLength);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetProductString(SafeFileHandle device, char* buffer, uint bufferLength);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_GetSerialNumberString(SafeFileHandle device, char* buffer, uint bufferLength);

    [LibraryImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    public static partial bool HidD_FlushQueue(SafeFileHandle device);

    // setupapi.dll

    [LibraryImport("setupapi.dll", SetLastError = true)]
    public static partial nint SetupDiGetClassDevsW(in Guid classGuid, nint enumerator, nint parent, uint flags);

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiEnumDeviceInterfaces(
        nint deviceInfoSet, nint deviceInfoData, in Guid interfaceClassGuid, uint memberIndex,
        ref SpDeviceInterfaceData deviceInterfaceData);

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiGetDeviceInterfaceDetailW(
        nint deviceInfoSet, ref SpDeviceInterfaceData deviceInterfaceData, byte* detail, uint detailSize,
        out uint requiredSize, SpDevinfoData* deviceInfoData);

    [LibraryImport("setupapi.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetupDiDestroyDeviceInfoList(nint deviceInfoSet);

    // cfgmgr32.dll

    [LibraryImport("cfgmgr32.dll")]
    public static partial uint CM_Get_Parent(out uint parent, uint device, uint flags);

    [LibraryImport("cfgmgr32.dll")]
    public static partial uint CM_Get_Device_IDW(uint device, char* buffer, uint bufferLength, uint flags);

    // kernel32.dll

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true,
        StringMarshalling = StringMarshalling.Utf16)]
    public static partial SafeFileHandle CreateFile(
        string fileName, uint desiredAccess, uint shareMode, nint securityAttributes, uint creationDisposition,
        uint flagsAndAttributes, nint templateFile);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool WriteFile(
        SafeFileHandle file, byte* buffer, uint length, uint* written, NativeOverlapped* overlapped);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReadFile(
        SafeFileHandle file, byte* buffer, uint length, uint* read, NativeOverlapped* overlapped);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetOverlappedResult(
        SafeFileHandle file, NativeOverlapped* overlapped, out uint transferred,
        [MarshalAs(UnmanagedType.Bool)] bool wait);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool CancelIoEx(SafeFileHandle file, NativeOverlapped* overlapped);
}

/// <summary><c>SP_DEVICE_INTERFACE_DATA</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SpDeviceInterfaceData
{
    public uint Size;
    public Guid InterfaceClassGuid;
    public uint Flags;
    public nuint Reserved;
}

/// <summary><c>SP_DEVINFO_DATA</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct SpDevinfoData
{
    public uint Size;
    public Guid ClassGuid;
    public uint DevInst;
    public nuint Reserved;
}

/// <summary><c>HIDD_ATTRIBUTES</c>.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct HiddAttributes
{
    public uint Size;
    public ushort VendorId;
    public ushort ProductId;
    public ushort VersionNumber;
}

/// <summary><c>HIDP_CAPS</c> (64 bytes).</summary>
[StructLayout(LayoutKind.Explicit, Size = 64)]
internal struct HidpCaps
{
    [FieldOffset(0)]
    public ushort Usage;

    [FieldOffset(2)]
    public ushort UsagePage;

    [FieldOffset(4)]
    public ushort InputReportByteLength;

    [FieldOffset(6)]
    public ushort OutputReportByteLength;

    [FieldOffset(52)]
    public ushort NumberOutputButtonCaps;

    [FieldOffset(54)]
    public ushort NumberOutputValueCaps;
}

/// <summary>
/// The head of <c>HIDP_VALUE_CAPS</c> and <c>HIDP_BUTTON_CAPS</c>, which share it and are both 72 bytes: the usage
/// page, then the report ID.
/// </summary>
[StructLayout(LayoutKind.Explicit, Size = 72)]
internal struct HidpReportCaps
{
    [FieldOffset(0)]
    public ushort UsagePage;

    [FieldOffset(2)]
    public byte ReportId;
}
