using System.Globalization;
using MINIKeyboard.Data.Protocols;
using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Connection;

/// <summary>
/// One top-level collection of a HID device, as Windows lists it. Windows makes a device of each top-level collection
/// of each USB interface; these are what its HID API tells of one without asking the keyboard anything.
/// </summary>
/// <param name="Path">The device interface path — what stays the same while the keyboard is plugged in.</param>
/// <param name="DeviceKey">Names the USB device the collection belongs to: the same for all its interfaces.</param>
/// <param name="Version"><c>bcdDevice</c> (<c>HidD_GetAttributes</c>).</param>
/// <param name="InterfaceNumber">
/// The USB interface, from <c>mi_XX</c> in the path; 0 when there is none — a device that isn't composite has one.
/// </param>
/// <param name="InputReportByteLength">
/// The longest input report, the report ID byte included; 0 when there is none.
/// </param>
/// <param name="OutputReportByteLength">
/// The longest output report, the report ID byte included; 0 when there is none.
/// </param>
/// <param name="OutputReportId">
/// The report ID of the first output item; 0 when the reports aren't numbered, null when there are no output items.
/// </param>
internal sealed record HidCollection(
    string Path,
    string DeviceKey,
    UsbIdentity Usb,
    ushort Version,
    byte InterfaceNumber,
    ushort UsagePage,
    ushort Usage,
    ushort InputReportByteLength,
    ushort OutputReportByteLength,
    byte? OutputReportId);

/// <summary>The strings a USB device may carry; these keyboards carry none.</summary>
internal readonly record struct DeviceStrings(string? Manufacturer, string? Product, string? SerialNumber);

/// <summary>
/// A place to write the configuration: a keyboard's configuration collection, and every collection of that keyboard.
/// </summary>
internal sealed record HidCandidate(
    HidCollection Collection, KeyboardFamily Family, IReadOnlyList<HidCollection> Device)
{
    /// <summary>What stays the same while the keyboard is plugged in.</summary>
    public string Entry => Collection.Path;

    /// <summary>The keyboard it is on: its USB device, which Windows names after the port it is plugged into.</summary>
    public AttachedKeyboard Keyboard => new(Collection.DeviceKey, Collection.Usb, Collection.DeviceKey);
}

/// <summary>
/// Which collection of which keyboard takes the configuration, and how — decided from what the HID API lists.
/// </summary>
internal static class HidCollections
{
    /// <summary>
    /// Every supported keyboard's configuration collection — the 1189:8890's interface 1, a 884x keyboard's interface
    /// 0 — in the order the vendor apps look for USB IDs.
    /// </summary>
    public static IReadOnlyList<HidCandidate> Candidates(IEnumerable<HidCollection> collections)
    {
        var found = new List<HidCandidate>();
        var devices = collections
            .GroupBy(collection => collection.DeviceKey, StringComparer.Ordinal)
            .OrderBy(device => device.Key, StringComparer.Ordinal);
        foreach (var device in devices)
        {
            var members = device.OrderBy(collection => collection.Path, StringComparer.Ordinal).ToList();
            if (KeyboardFamilies.Of(members[0].Usb) is not { } family)
                continue;
            if (ConfigurationCollection(members, family) is { } configuration)
                found.Add(new HidCandidate(configuration, family, members));
        }
        return [.. found.OrderBy(candidate => SearchOrder(candidate.Collection.Usb))];
    }

    /// <summary>
    /// Among the collections of the family's configuration interface, the one with an output report — a vendor-defined
    /// one (usage page from 0xFF00) before any other.
    /// </summary>
    public static HidCollection? ConfigurationCollection(IEnumerable<HidCollection> device, KeyboardFamily family) =>
        device
            .Where(collection => collection.InterfaceNumber == family.ConfigurationInterface())
            .Where(collection => collection.OutputReportByteLength > 0)
            .OrderBy(collection => collection.UsagePage >= 0xFF00 ? 0 : 1)
            .ThenBy(collection => collection.Path, StringComparer.Ordinal)
            .FirstOrDefault();

    /// <summary>
    /// The report ID the output caps name — 0 where the reports aren't numbered — or 3, which both vendor tools send,
    /// where there are none. The length is the report as the keyboard takes it: Windows counts a report ID byte even
    /// for unnumbered reports, which the app's reports don't carry.
    /// </summary>
    public static ReportFraming Framing(HidCollection collection)
    {
        var reportId = collection.OutputReportId ?? K884x.ReportId;
        return reportId == 0
            ? new ReportFraming(0, collection.OutputReportByteLength - 1)
            : new ReportFraming(reportId, collection.OutputReportByteLength);
    }

    /// <summary>
    /// Whether answers are read: a 884x keyboard answers; the 1189:8890's configuration interface has no IN endpoint,
    /// whatever its report descriptor declares, so nothing would come.
    /// </summary>
    public static bool ReadsAnswers(HidCandidate candidate) =>
        candidate.Family == KeyboardFamily.K884x && candidate.Collection.InputReportByteLength > 0;

    /// <summary>
    /// The bytes <c>WriteFile</c> takes for a report as framed: Windows wants the report ID first even when it is 0,
    /// and every report as long as the longest.
    /// </summary>
    public static byte[] OutputBuffer(byte[] report, ReportFraming framing, int outputReportByteLength)
    {
        var prefix = framing.ReportId == 0 ? 1 : 0;
        var buffer = new byte[Math.Max(outputReportByteLength, prefix + report.Length)];
        report.CopyTo(buffer, prefix);
        return buffer;
    }

    /// <summary>
    /// What the HID API tells of the keyboard: its release and strings, and one interface per <c>mi_XX</c> with the
    /// usage of each of its collections. The USB version, speed, power, subclass and protocol it doesn't tell.
    /// </summary>
    public static DeviceDetails Details(HidCandidate candidate, DeviceStrings strings)
    {
        var interfaces = candidate.Device
            .GroupBy(collection => collection.InterfaceNumber)
            .OrderBy(group => group.Key)
            .Select(group => new DeviceInterface(
                group.Key,
                Class: 3,
                Subclass: null,
                Protocol: null,
                new EquatableArray<UsagePair>(group
                    .OrderBy(collection => collection.Path, StringComparer.Ordinal)
                    .Select(collection => new UsagePair(collection.UsagePage, collection.Usage))
                    .Distinct())));
        return new DeviceDetails(
            UsbVersion: null,
            DeviceRelease: candidate.Collection.Version,
            Speed: null,
            strings.Manufacturer,
            strings.Product,
            strings.SerialNumber,
            MaxPower: null,
            new EquatableArray<DeviceInterface>(interfaces));
    }

    /// <summary>
    /// The USB interface in a HID device path: <c>\\?\hid#vid_1189&amp;pid_8890&amp;mi_01#…</c> is interface 1. Null
    /// when the path names none — a device with one interface isn't composite and gets no <c>mi_XX</c>.
    /// </summary>
    public static byte? InterfaceNumber(string path)
    {
        var index = path.IndexOf("&mi_", StringComparison.OrdinalIgnoreCase);
        if (index < 0 || index + 6 > path.Length)
            return null;
        return byte.TryParse(path.AsSpan(index + 4, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture,
            out var number)
            ? number
            : null;
    }

    private static int SearchOrder(UsbIdentity usb)
    {
        for (var index = 0; index < KeyboardFamilies.Supported.Count; index++)
        {
            if (KeyboardFamilies.Supported[index] == usb)
                return index;
        }
        return int.MaxValue;
    }
}
