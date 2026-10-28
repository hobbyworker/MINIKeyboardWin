namespace MINIKeyboard.Domain.Tests;

public class KeyboardsTests
{
    private static readonly CommandSet K8890 = new CommandSet.Packets(3);
    private static readonly CommandSet Standard = new CommandSet.Records(RecordFormat.Standard);
    private static readonly CommandSet Legacy = new CommandSet.Records(RecordFormat.Legacy);
    private static readonly CommandSet Unidentified = new CommandSet.Records(RecordFormat.Unidentified);

    private static KeyboardCapabilities Capabilities(CommandSet commands) => new(commands);

    private static KeyMacro Macro(params KeyStroke[] strokes) => new([.. strokes]);

    [Fact]
    public void SupportedKeyboards()
    {
        Assert.Equal(KeyboardFamily.K8890, KeyboardFamilies.Of(new UsbIdentity(0x1189, 0x8890)));
        Assert.Equal(KeyboardFamily.K884x, KeyboardFamilies.Of(new UsbIdentity(0x1189, 0x8840)));
        Assert.Equal(KeyboardFamily.K884x, KeyboardFamilies.Of(new UsbIdentity(0x514C, 0x8851)));
        Assert.Equal(KeyboardFamily.K884x, KeyboardFamilies.Of(new UsbIdentity(0x1189, 0x8851)));
        Assert.Null(KeyboardFamilies.Of(new UsbIdentity(0x05AC, 0x8890)));
        Assert.Equal(17, KeyboardFamilies.Supported.Count);
        Assert.Equal(17, KeyboardFamilies.Supported.Distinct().Count());
        ushort[] products = [0x8842, 0x8840, 0x8830, 0x8831, 0x8832, 0x8833, 0x8850, 0x8851];
        Assert.Equal(
            [new(0x1189, 0x8890), .. products.Select(p => new UsbIdentity(0x1189, p)), .. products.Select(p => new UsbIdentity(0x514C, p))],
            KeyboardFamilies.Supported.ToArray());
    }

    [Fact]
    public void UsbIdentityPrintsAsHex() => Assert.Equal("514C:8850", new UsbIdentity(0x514C, 0x8850).ToString());

    [Fact]
    public void IdentityPicksTheRecordFormat()
    {
        Assert.Equal(RecordFormat.Standard, new KeyboardIdentity(6, 1, 10).RecordFormat);
        Assert.Equal(RecordFormat.Standard, new KeyboardIdentity(12, 2, 11).RecordFormat);
        Assert.Equal(RecordFormat.Standard, new KeyboardIdentity(12, 2, 12).RecordFormat);
        Assert.Equal(RecordFormat.Standard, new KeyboardIdentity(12, 2, 255).RecordFormat);
        Assert.Equal(RecordFormat.Legacy, new KeyboardIdentity(6, 1, 0).RecordFormat);
        Assert.Equal(RecordFormat.Legacy, new KeyboardIdentity(6, 1, 1).RecordFormat);
        Assert.Equal(RecordFormat.Legacy, new KeyboardIdentity(6, 1, 9).RecordFormat);
    }

    /// <summary>The types the app was tried with; any other is handled alike, but marked.</summary>
    [Fact]
    public void KnownTypes()
    {
        Assert.All(new byte[] { 0, 1, 10, 11 }, kind => Assert.True(new KeyboardIdentity(12, 2, kind).IsKnownKind));
        Assert.All(new byte[] { 2, 9, 12, 255 }, kind => Assert.False(new KeyboardIdentity(12, 2, kind).IsKnownKind));
        Assert.Equal(RecordFormat.Standard, new KeyboardIdentity(12, 2, 12).RecordFormat);
    }

    [Fact]
    public void Layers()
    {
        Assert.Equal(3, Capabilities(K8890).LayerCount);
        Assert.Equal(1, Capabilities(new CommandSet.Packets(0)).LayerCount);
        Assert.Equal(3, Capabilities(Standard).LayerCount);
    }

    /// <summary>The standard format spends a place on every modifier; the others put a key's modifiers with it.</summary>
    [Fact]
    public void PressesCountModifiersOnTheStandardFormat()
    {
        var macro = Macro(new KeyStroke(Modifiers.Super | Modifiers.Shift, 0x1D), KeyStroke.OfKey(0x04));
        Assert.Equal(4, Capabilities(Standard).Presses(macro));
        Assert.Equal(2, Capabilities(Legacy).Presses(macro));
        Assert.Equal(2, Capabilities(K8890).Presses(macro));
        var modifiersOnly = Macro(new KeyStroke(Modifiers.Control | Modifiers.Alt, 0));
        Assert.Equal(2, Capabilities(Standard).Presses(modifiersOnly));
    }

    [Fact]
    public void Limits()
    {
        var five = Macro([.. Enumerable.Repeat(KeyStroke.OfKey(0x04), 5)]);
        var six = Macro([.. Enumerable.Repeat(KeyStroke.OfKey(0x04), 6)]);
        Assert.True(Capabilities(K8890).Fits(five));
        Assert.False(Capabilities(K8890).Fits(six));
        Assert.True(Capabilities(Legacy).Fits(Macro([.. Enumerable.Repeat(KeyStroke.OfKey(0x04), 20)])));
        Assert.False(Capabilities(Standard).Fits(Macro([.. Enumerable.Repeat(new KeyStroke(Modifiers.Super, 0x04), 10)])));
        Assert.False(Capabilities(K8890).Fits(Macro(KeyStroke.OfKey(0x04), new KeyStroke(Modifiers.Shift, 0))));
    }

    [Fact]
    public void WhatEachKeyboardDoes()
    {
        Assert.Equal(MediaKey.Basic, Capabilities(K8890).MediaKeys);
        Assert.Equal(21, Capabilities(Standard).MediaKeys.Count);
        Assert.Equal(DelaySupport.Unavailable, Capabilities(K8890).Delays);
        Assert.Equal(new DelaySupport.PerPress(new(0, 60000)), Capabilities(Standard).Delays);
        Assert.Equal(new DelaySupport.PerKey(new(0, 6000)), Capabilities(Legacy).Delays);
        Assert.Equal(DelaySupport.Unavailable, Capabilities(Unidentified).Delays);
        Assert.Equal(new LightingSupport.Modes([0, 1, 2]), Capabilities(K8890).Lighting);
        Assert.Equal(new LightingSupport.PerKeyColor(new(0, 5), 16), Capabilities(Standard).Lighting);
        Assert.Equal(new LightingSupport.ColorAndMode(new(0, 5)), Capabilities(Legacy).Lighting);
        Assert.Equal(ReadSupport.OnConnect, Capabilities(Standard).Reading);
        Assert.Equal(ReadSupport.OnConnect, Capabilities(Legacy).Reading);
        Assert.Equal(ReadSupport.Never, Capabilities(K8890).Reading);
        Assert.Equal(ReadSupport.Never, Capabilities(Unidentified).Reading);
    }

    /// <summary>The 8890 numbers twelve keys and two knobs.</summary>
    [Fact]
    public void The8890AddressesSmallLayoutsOnly()
    {
        static KeyboardLayout Named(string id) => KeyboardLayout.Named(id)!;
        Assert.True(Capabilities(K8890).Addresses(Named("6add1")));
        Assert.True(Capabilities(K8890).Addresses(Named("12add2")));
        Assert.False(Capabilities(K8890).Addresses(Named("15add3")));
        Assert.False(Capabilities(K8890).Addresses(Named("4add3")));
        Assert.False(Capabilities(K8890).Addresses(Named("0add1")));
        Assert.True(Capabilities(Unidentified).Addresses(Named("21add1")));
    }

    /// <summary>The keys each vendor tool offers: 95 on the Windows tool, 110 on the cross-platform app.</summary>
    [Fact]
    public void KeysEachKeyboardIsGiven()
    {
        var k8890 = Capabilities(K8890);
        var standard = Capabilities(Standard);
        var every = Enumerable.Range(0, 256).Select(key => (byte)key).ToArray();
        Assert.Equal(95, every.Count(k8890.Takes));
        Assert.Equal(110, every.Count(standard.Takes));
        Assert.All(every, key => Assert.Equal(Capabilities(Legacy).Takes(key), Capabilities(Unidentified).Takes(key)));
        foreach (byte key in new byte[] { 0x32, 0x58, 0x64, 0x68, 0x73 })
            Assert.True(!k8890.Takes(key) && standard.Takes(key), $"0x{key:x}");
        Assert.True(k8890.Takes(0x65) && !k8890.Takes(0x66) && !standard.Takes(0x66) && !standard.Takes(0x74));
        Assert.True(k8890.TakesKeysOf(Macro(new KeyStroke(Modifiers.Shift, 0))));
        Assert.False(k8890.TakesKeysOf(Macro(KeyStroke.OfKey(0x04), KeyStroke.OfKey(0x68))));
    }

    /// <summary>The 8890 has twelve keys and two knobs that turn and press.</summary>
    [Fact]
    public void WhatThe8890Takes()
    {
        var k8890 = Capabilities(K8890);
        Assert.True(k8890.CanAddress(ControlId.Key(12)));
        Assert.False(k8890.CanAddress(ControlId.Key(13)));
        Assert.True(k8890.CanAddress(ControlId.Knob(2, KnobPart.RotateRight)));
        Assert.False(k8890.CanAddress(ControlId.Knob(3, KnobPart.Press)));
        Assert.False(k8890.CanAddress(ControlId.Knob(1, KnobPart.PressRotateLeft)));
        Assert.True(Capabilities(Standard).CanAddress(ControlId.Knob(1, KnobPart.PressRotateLeft)));
    }

    [Fact]
    public void MouseActionsComeBackFromTheirParts()
    {
        foreach (var action in MouseActions.All)
            Assert.Equal(action, MouseActions.FromParts(action.Buttons(), action.Wheel(), action.Modifiers()));
        Assert.Null(MouseActions.FromParts(0, 0, Modifiers.None));
    }
}
