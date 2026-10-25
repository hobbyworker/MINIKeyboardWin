using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation.Tests;

/// <summary>The drawing and the tables, from inside the assembly.</summary>
public class PresentationTests
{
    private static LayoutGeometry Geometry(string id) => LayoutGeometry.Of(KeyboardLayout.Named(id)!)!;

    private static GridShape Shape(byte keys, byte columns, byte knobs) => GridShape.Create(keys, columns, knobs)!.Value;

    /// <summary>A knob is one body in the shape, on its press action.</summary>
    [Fact]
    public void KnobsAreOneBodyEach()
    {
        var sixAndOne = Geometry("6add1");
        var press = sixAndOne.Rects[ControlId.Knob(1, KnobPart.Press)];
        Assert.Equal([((byte)1, press)], sixAndOne.KnobOutlines());
        Assert.Single(Geometry("0add1").KnobOutlines());
        foreach (var layout in KeyboardLayout.All)
        {
            var geometry = LayoutGeometry.Of(layout);
            Assert.True(geometry is not null, layout.Id);
            Assert.True(geometry.Rects.Keys.ToHashSet().SetEquals(layout.Controls), layout.Id);
            Assert.True(geometry.KnobOutlines().Count == layout.KnobCount, layout.Id);
        }
    }

    /// <summary>
    /// The app's layouts with their turns redrawn: one size in a layout, each turn the gap off its knob, none over
    /// another control or out of the body.
    /// </summary>
    [Fact]
    public void TurnsAreEvenAndClear()
    {
        static double Distance(LayoutRect a, LayoutRect b) =>
            Math.Sqrt((a.MidX - b.MidX) * (a.MidX - b.MidX) + (a.MidY - b.MidY) * (a.MidY - b.MidY));
        foreach (var layout in KeyboardLayout.All.Where(layout => GridShape.FromId(layout.Id) is null))
        {
            var geometry = LayoutGeometry.Of(layout)!;
            var turns = geometry.Rects.Where(pair => !pair.Key.IsKey && pair.Key.Part != KnobPart.Press).ToList();
            foreach (var (control, turn) in turns)
            {
                var name = $"{layout.Id} {control}";
                Assert.True(Math.Abs(turn.Width - turns[0].Value.Width) < 1e-9 && turn.Height == turn.Width, name);
                var press = geometry.Rects[ControlId.Knob(control.Number, KnobPart.Press)];
                var gap = Distance(turn, press) - press.Width / 2 - turn.Width / 2;
                Assert.True(Math.Abs(gap - LayoutGeometry.TurnGap) < 1e-6, name);
                foreach (var (other, rect) in geometry.Rects)
                {
                    if (other == control)
                        continue;
                    if (other.IsKey)
                    {
                        var x = Math.Clamp(turn.MidX, rect.X, rect.MaxX);
                        var y = Math.Clamp(turn.MidY, rect.Y, rect.MaxY);
                        var reach = Math.Sqrt((turn.MidX - x) * (turn.MidX - x) + (turn.MidY - y) * (turn.MidY - y));
                        Assert.True(reach >= turn.Width / 2, $"{name} over {other}");
                    }
                    else
                    {
                        var clear = Distance(turn, rect) >= (turn.Width + rect.Width) / 2 - 1e-6;
                        Assert.True(clear, $"{name} over {other}");
                    }
                }
                var frame = geometry.Frame;
                var inside = turn.X >= frame.X && turn.Y >= frame.Y && turn.MaxX <= frame.MaxX && turn.MaxY <= frame.MaxY;
                Assert.True(inside, name);
            }
        }
    }

    [Fact]
    public void RotationTurnsTheGeometry()
    {
        var geometry = Geometry("6add1");
        var quarter = geometry.Rotated(Rotation.Quarter);
        Assert.True(Math.Abs(quarter.Bounds().Width - geometry.Bounds().Height) < 0.001);
        Assert.True(Math.Abs(quarter.Bounds().Height - geometry.Bounds().Width) < 0.001);
        var full = quarter.Rotated(Rotation.Quarter).Rotated(Rotation.Quarter).Rotated(Rotation.Quarter);
        foreach (var (control, rect) in geometry.Rects)
        {
            var back = full.Rects[control];
            Assert.True(Math.Abs(back.MidX - rect.MidX) < 0.001 && Math.Abs(back.MidY - rect.MidY) < 0.001);
        }
        var upright = geometry.Rotated(Rotation.Upright);
        Assert.Equal(geometry.Frame, upright.Frame);
        Assert.Equal(geometry.Rects, upright.Rects);
    }

    /// <summary>
    /// An arrangement: keys fill the rows from the top left; a knob stands at the middle of the keys, two at the first
    /// and last rows, each with its turns either side of its press.
    /// </summary>
    [Fact]
    public void Arrangements()
    {
        var seven = LayoutGeometry.Arrangement(Shape(7, 3, 2));
        var (key1, key4) = (seven.Rects[ControlId.Key(1)], seven.Rects[ControlId.Key(4)]);
        Assert.True(key4.X == key1.X && key4.Y > key1.Y);
        Assert.Equal(key1.X, seven.Rects[ControlId.Key(7)].X);

        var one = LayoutGeometry.Arrangement(Shape(6, 3, 1));
        var keys = one.Rects.Where(pair => pair.Key.IsKey).Select(pair => pair.Value).ToList();
        var middle = (keys.Min(rect => rect.Y) + keys.Max(rect => rect.MaxY)) / 2;
        var press = one.Rects[ControlId.Knob(1, KnobPart.Press)];
        Assert.True(Math.Abs(press.MidY - middle) < 0.001);
        Assert.Equal(press.MidY, one.Rects[ControlId.Knob(1, KnobPart.RotateLeft)].MidY);

        var two = LayoutGeometry.Arrangement(Shape(12, 4, 2));
        Assert.True(
            Math.Abs(two.Rects[ControlId.Knob(1, KnobPart.Press)].MidY - two.Rects[ControlId.Key(1)].MidY) < 0.001);
        Assert.True(
            Math.Abs(two.Rects[ControlId.Knob(2, KnobPart.Press)].MidY - two.Rects[ControlId.Key(9)].MidY) < 0.001);
    }

    /// <summary>Nothing overlaps, however the keyboard is turned, and the body holds everything.</summary>
    [Fact]
    public void ArrangementsDontOverlap()
    {
        static bool Overlaps(LayoutRect a, LayoutRect b) => a.X < b.MaxX && b.X < a.MaxX && a.Y < b.MaxY && b.Y < a.MaxY;
        for (byte keys = 0; keys <= 12; keys++)
        {
            for (byte columns = 1; columns <= Math.Max(keys, (byte)1); columns++)
            {
                for (byte knobs = 0; knobs <= 2; knobs++)
                {
                    if (GridShape.Create(keys, columns, knobs) is not { } shape)
                        continue;
                    foreach (var rotation in Rotations.All)
                    {
                        var geometry = LayoutGeometry.Arrangement(shape).Rotated(rotation);
                        Assert.True(geometry.Rects.Keys.ToHashSet().SetEquals(shape.Layout().Controls), shape.Id);
                        var rects = geometry.Rects.Values.ToList();
                        for (var i = 0; i < rects.Count; i++)
                        {
                            for (var j = i + 1; j < rects.Count; j++)
                                Assert.False(Overlaps(rects[i], rects[j]), $"{shape.Id} {rotation}");
                            Assert.True(
                                rects[i].X >= geometry.Frame.X && rects[i].MaxX <= geometry.Frame.MaxX + 0.001,
                                shape.Id);
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void Strokes()
    {
        var stroke = new KeyStroke(Modifiers.Super | Modifiers.Shift | Modifiers.Control, 0x1D);
        Assert.Equal("Ctrl+Shift+Win+Z", ActionText.Stroke(stroke));
        Assert.Equal("Alt+RWin", ActionText.Stroke(new KeyStroke(Modifiers.Alt | Modifiers.RightSuper, 0)));
        Assert.Equal("A ↵", ActionText.Keys(new KeyMacro([KeyStroke.OfKey(0x04), KeyStroke.OfKey(0x28)])));
        Assert.Equal("0xA0", ActionText.Key(0xA0));
        Assert.Equal("Win", ModifierKey.Super.Label());
        Assert.Equal("RWin", ModifierKey.RightSuper.Label());
        Assert.Equal("RCtrl", ModifierKey.RightControl.Label());
    }

    [Fact]
    public void KeyUsagesAreUniqueAndNamed()
    {
        Assert.Equal(KeyUsage.All.Count, KeyUsage.All.Select(key => key.Usage).Distinct().Count());
        Assert.Equal("A", KeyUsage.Named(0x04)?.Label);
        Assert.Equal("F12", KeyUsage.Named(0x45)?.Label);
        Assert.Equal("F13", KeyUsage.Named(0x68)?.Label);
        Assert.Equal("F24", KeyUsage.Named(0x73)?.Label);
        Assert.Null(KeyUsage.Named(0));
    }

    /// <summary>
    /// The full-size keyboard has every key but those a full-size keyboard doesn't, each once, and no two caps overlap
    /// or run past the row.
    /// </summary>
    [Fact]
    public void TheFullKeyboard()
    {
        var keys = FullKeyboard.Places
            .Select(place => place.Face)
            .OfType<BoardFace.Key>()
            .Select(face => face.Usage.Usage)
            .ToList();
        var elsewhere = KeyUsage.ExtraFunctionKeys.Concat(KeyUsage.International).Select(key => key.Usage).ToHashSet();
        Assert.Equal(keys.Count, keys.Distinct().Count());
        var all = KeyUsage.All.Select(key => key.Usage).ToHashSet();
        all.ExceptWith(elsewhere);
        Assert.True(all.SetEquals(keys));
        var modifiers = FullKeyboard.Places
            .Select(place => place.Face)
            .OfType<BoardFace.Modifier>()
            .Select(face => face.ModifierKey)
            .ToHashSet();
        Assert.True(modifiers.SetEquals(ModifierKeys.All));
        for (var row = 0; row < 6; row++)
        {
            var caps = FullKeyboard.Places.Where(place => place.Row == row).OrderBy(place => place.Column).ToList();
            Assert.True(
                caps.Zip(caps.Skip(1)).All(pair => pair.First.Column + pair.First.Width <= pair.Second.Column),
                $"row {row}");
            Assert.True(caps.Count == 0 || caps[^1].Column + caps[^1].Width <= BoardCap.RowWidth, $"row {row}");
        }
    }

    /// <summary>A report in the device log: up to its zero padding on one line, every byte 16 to a row.</summary>
    [Fact]
    public void Packets()
    {
        var report = new byte[65];
        report[0] = 0x03;
        report[1] = report[2] = report[3] = 0xFB;
        Assert.Equal("03 FB FB FB", PacketText.Summary(report));
        Assert.Equal(4, PacketText.SummaryCount(report));
        var rows = PacketText.Rows(report);
        Assert.Equal(5, rows.Count);
        Assert.Equal("0000  03 FB FB FB 00 00 00 00 00 00 00 00 00 00 00 00", rows[0]);
        Assert.Equal("0040  00", rows[4]);
        Assert.Equal("00", PacketText.Summary([0, 0]));
        Assert.Empty(PacketText.Rows([]));
    }

    /// <summary>Every media key and mouse action the domain knows has a name on screen.</summary>
    [Fact]
    public void ChoicesCoverTheDomain()
    {
        Assert.All(MediaKey.All, key => Assert.NotNull(key.Choice()));
        Assert.Equal(MediaKey.All.Count, MediaChoices.All.Count);
        Assert.Equal(MouseChoices.All.Count, MouseActions.All.Select(action => action.Choice()).Distinct().Count());
        Assert.Equal(new ActionCaption.OtherMedia(0x999), ActionCaption.Of(new KeyAction.Media(new MediaKey(0x999))));
        Assert.Equal(new ActionCaption.Unknown(), ActionCaption.Of(null));
        Assert.Equal("#00FF00", new Rgb(0, 255, 0).Hex());
        Assert.Equal(56, Rgb.Palette.Count);
    }

    /// <summary>The information popover, from what a 1189:8890 reports.</summary>
    [Fact]
    public void KeyboardInformation()
    {
        static DeviceInterface Interface(byte number, byte subclass, byte protocol, params UsagePair[] usages) =>
            new(number, 3, subclass, protocol, [.. usages]);
        var details = new DeviceDetails(
            0x0110,
            0,
            12_000_000,
            null,
            null,
            null,
            100,
            [
                Interface(0, 1, 1, new UsagePair(1, 6)),
                Interface(1, 0, 0),
                Interface(2, 0, 0, new UsagePair(1, 6), new UsagePair(0x0C, 1)),
                Interface(3, 1, 2),
            ]);
        var keyboard = new ConnectedKeyboard(
            KeyboardFamilies.K8890Usb, KeyboardFamily.K8890, 1, Channel.Hid, 3, 65, details);
        var identification = new KeyboardIdentification(new CommandSet.Packets(3), null);
        var info = KeyboardInfo.Of(keyboard, identification);
        Assert.Equal("1189:8890", info.UsbId);
        Assert.Equal(new Firmware.Unknown(), info.Firmware);
        Assert.Equal("USB 1.1 · 12 Mb/s", info.Usb);
        Assert.Equal(
            [
                [InterfaceRole.Keyboard], [InterfaceRole.Configuration], [InterfaceRole.Keyboard, InterfaceRole.Media],
                [InterfaceRole.Mouse],
            ],
            info.Interfaces.Select(face => face.Roles.ToArray()));
        Assert.Equal([false, true, false, false], info.Interfaces.Select(face => face.IsConfiguration));
        Assert.Equal(new InfoChannel(1, ChannelPath.Hid, 3, 65), info.Channel);
        Assert.Equal(new InfoCommands.Packets(3), info.Commands);

        // The HID API tells no USB version, nor an interface's subclass and protocol.
        Assert.Equal("12 Mb/s", KeyboardInfo.UsbText(details with { UsbVersion = null }));
        Assert.Null(KeyboardInfo.UsbText(details with { UsbVersion = null, Speed = null }));
        Assert.Equal([InterfaceRole.Other], KeyboardInfo.Roles(new DeviceInterface(3, 3, null, null, []), 1));
        var released = keyboard with { Details = details with { DeviceRelease = 0x0102 } };
        Assert.Equal(new Firmware.Release("1.02"), KeyboardInfo.Of(released, null).Firmware);
    }
}
