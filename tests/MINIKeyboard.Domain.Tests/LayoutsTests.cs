using MINIKeyboard.Domain.UseCases;

namespace MINIKeyboard.Domain.Tests;

public class LayoutsTests
{
    private sealed class Profiles : ILayoutProfileRepository
    {
        private readonly Dictionary<UsbIdentity, LayoutProfile> saved = [];

        public LayoutProfile? Profile(UsbIdentity keyboard) => saved.GetValueOrDefault(keyboard);

        public void Save(LayoutProfile profile, UsbIdentity keyboard) => saved[keyboard] = profile;
    }

    private static ConnectedKeyboard Keyboard(ushort product, KeyboardFamily family) =>
        new(new UsbIdentity(0x1189, product), family, 0, Channel.Hid, 3, 65, null);

    private static KeyboardLayout Named(string id) => KeyboardLayout.Named(id)!;

    [Fact]
    public void EveryLayoutIsConsistent()
    {
        Assert.Equal(26, KeyboardLayout.All.Count);
        Assert.Equal(26, KeyboardLayout.All.Select(layout => layout.Id).Distinct().Count());
        foreach (var layout in KeyboardLayout.All)
        {
            Assert.Equal(layout.Controls.Count, layout.Controls.Distinct().Count());
            var keys = layout.Controls.Where(control => control.IsKey).Select(control => (int)control.Number).ToArray();
            Assert.Equal(Enumerable.Range(1, keys.Length), keys);
            var knobs = layout.Controls.Select(control => control.KnobNumber).OfType<byte>().Distinct().Order()
                .Select(knob => (int)knob);
            Assert.Equal(Enumerable.Range(1, layout.KnobCount), knobs);
        }
    }

    /// <summary><c>Widget::Identify_KeyBoard_style</c>.</summary>
    [Fact]
    public void IdentifiedLayouts()
    {
        static string Id(byte keys, byte knobs, byte kind, ushort product) =>
            KeyboardLayout.IdentifiedId(new KeyboardIdentity(keys, knobs, kind), product);
        Assert.Equal("6add1", Id(6, 1, 10, 0x8840));
        Assert.Equal("0add1", Id(0, 2, 10, 0x8840));
        Assert.Equal("0add2", Id(0, 2, 10, 0x8850));
        Assert.Equal("4add1", Id(4, 1, 0, 0x8840));
        Assert.Equal("4add2", Id(4, 1, 0, 0x8851));
        Assert.Equal("4add2", Id(4, 1, 1, 0x8840));
        Assert.Equal("15add3", Id(4, 1, 10, 0x8840));
        Assert.Equal("15add3", Id(7, 7, 10, 0x8840));
        Assert.All(KeyboardLayout.All, layout => Assert.NotNull(KeyboardLayout.Named(layout.Id)));
    }

    /// <summary>One knob that also turns while pressed — not two knobs.</summary>
    [Fact]
    public void TheDialHasOneKnob()
    {
        var dial = Named("0add1");
        Assert.Equal(1, dial.KnobCount);
        Assert.True(dial.Has(ControlId.Knob(1, KnobPart.PressRotateLeft)));
        Assert.True(dial.Has(ControlId.Knob(1, KnobPart.PressRotateRight)));
        Assert.Equal(2, Named("0add2").KnobCount);
    }

    [Fact]
    public void RotationTurnsAround()
    {
        Assert.Equal(Rotation.Upright, Rotation.ThreeQuarters.Next());
        Assert.Equal(Rotation.Quarter, Rotation.Upright.Next());
        Assert.Equal(Rotation.ThreeQuarters, Rotations.FromQuarterTurns(-1));
        Assert.Equal(Rotation.Half, Rotations.FromQuarterTurns(6));
    }

    [Fact]
    public void AnIdentifiedKeyboardGetsItsLayout()
    {
        var identification = new KeyboardIdentification(
            new CommandSet.Records(RecordFormat.Standard), new KeyboardIdentity(9, 2, 10));
        var resolved = new ResolveLayoutUseCase(new Profiles())
            .Execute(Keyboard(0x8840, KeyboardFamily.K884x), identification);
        Assert.Equal("9add2", resolved.Layout.Id);
        Assert.Equal(Source.Identified, resolved.Source);
        Assert.True(resolved.Choices.IsEmpty);
        Assert.False(resolved.NeedsChoice);
    }

    /// <summary>
    /// The cross-platform app doesn't know the 8890 and its own tool draws no shape, so the user arranges it. Until
    /// then it is drawn as the tool's twelve keys, four to a row, and two knobs.
    /// </summary>
    [Fact]
    public void The8890IsArranged()
    {
        var profiles = new Profiles();
        var keyboard = Keyboard(0x8890, KeyboardFamily.K8890);
        var identification = new KeyboardIdentification(new CommandSet.Packets(3), null);
        var resolve = new ResolveLayoutUseCase(profiles);
        var choose = new ChooseLayoutUseCase(profiles);
        var resolved = resolve.Execute(keyboard, identification);
        Assert.Equal("grid-12-4-2", resolved.Layout.Id);
        Assert.True(resolved.NeedsChoice);
        Assert.True(resolved.Choices.IsEmpty);
        Assert.Equal(GridLimits.K8890, resolved.Grid);

        var sixAndOne = GridShape.Create(6, 3, 1)!.Value.Layout();
        var chosen = choose.Execute(resolved, sixAndOne, Rotation.Half, keyboard.Usb);
        Assert.Equal(sixAndOne, chosen.Layout);
        Assert.Equal(Rotation.Half, chosen.Rotation);
        Assert.Equal(Source.Chosen, chosen.Source);

        var again = resolve.Execute(keyboard, identification);
        Assert.Equal(sixAndOne, again.Layout);
        Assert.Equal(Rotation.Half, again.Rotation);
        Assert.False(again.NeedsChoice);

        // Neither an arrangement it can't number nor one of the cross-platform app's layouts.
        var tooBig = GridShape.Create(13, 4, 0)!.Value.Layout();
        Assert.Equal(sixAndOne, choose.Execute(again, tooBig, Rotation.Upright, keyboard.Usb).Layout);
        Assert.Equal(sixAndOne, choose.Execute(again, Named("6add1"), Rotation.Upright, keyboard.Usb).Layout);
    }

    [Fact]
    public void AnUnidentifiedKeyboardMayHaveAnyLayout()
    {
        var identification = new KeyboardIdentification(new CommandSet.Records(RecordFormat.Unidentified), null);
        var resolved = new ResolveLayoutUseCase(new Profiles())
            .Execute(Keyboard(0x8840, KeyboardFamily.K884x), identification);
        Assert.Equal(26, resolved.Choices.Count);
        Assert.Equal("15add3", resolved.Layout.Id);
        Assert.True(resolved.NeedsChoice);
    }

    /// <summary>The layout of a keyboard that said its shape stays; only the rotation is kept.</summary>
    [Fact]
    public void AnIdentifiedLayoutOnlyTurns()
    {
        var profiles = new Profiles();
        var keyboard = Keyboard(0x8840, KeyboardFamily.K884x);
        var identification = new KeyboardIdentification(
            new CommandSet.Records(RecordFormat.Legacy), new KeyboardIdentity(6, 1, 0));
        var resolve = new ResolveLayoutUseCase(profiles);
        var resolved = resolve.Execute(keyboard, identification);
        var chosen = new ChooseLayoutUseCase(profiles).Execute(resolved, Named("12add0"), Rotation.Quarter, keyboard.Usb);
        Assert.Equal("6add1", chosen.Layout.Id);
        Assert.Equal(Rotation.Quarter, chosen.Rotation);
        Assert.Equal(Rotation.Quarter, resolve.Execute(keyboard, identification).Rotation);
    }

    [Fact]
    public void GridIds()
    {
        var shape = GridShape.Create(6, 3, 1)!.Value;
        Assert.Equal("grid-6-3-1", shape.Id);
        Assert.Equal(shape, GridShape.FromId("grid-6-3-1"));
        Assert.Null(GridShape.FromId("6add1"));
        Assert.Null(GridShape.FromId("grid-6-3-1-0"));
        Assert.Null(GridShape.Create(0, 1, 0));
        Assert.Null(GridShape.Create(6, 7, 0));
        Assert.NotNull(GridShape.Create(0, 1, 2));
        Assert.Equal(3, GridShape.Create(7, 3, 0)!.Value.Rows);
    }

    /// <summary>Keys first, numbered from 1, then each knob's three actions.</summary>
    [Fact]
    public void GridControls()
    {
        var layout = GridShape.Create(7, 3, 2)!.Value.Layout();
        Assert.Equal(Enumerable.Range(1, 7).Select(number => ControlId.Key((byte)number)), layout.Controls.Take(7));
        Assert.Equal(
            [
                ControlId.Knob(2, KnobPart.RotateLeft), ControlId.Knob(2, KnobPart.Press),
                ControlId.Knob(2, KnobPart.RotateRight),
            ],
            layout.Controls.TakeLast(3));
        Assert.True(layout.KeyCount == 7 && layout.KnobCount == 2);
        var k8890 = new KeyboardCapabilities(new CommandSet.Packets(3));
        Assert.True(k8890.Addresses(layout));
        Assert.False(k8890.Addresses(GridShape.Create(13, 4, 0)!.Value.Layout()));
    }

    [Fact]
    public void EverySimulatedModelHasAKeyboard()
    {
        var all = SimulatedModel.All();
        Assert.True(all.Count > 40);
        Assert.Equal(all.Count, all.Select(model => model.Id).Distinct().Count());
        Assert.All(all, model => Assert.NotNull(KeyboardFamilies.Of(model.Usb)));
        Assert.Null(SimulatedModel.IdentifiedAs(Named("4add1"), RecordFormat.Standard));
    }
}
