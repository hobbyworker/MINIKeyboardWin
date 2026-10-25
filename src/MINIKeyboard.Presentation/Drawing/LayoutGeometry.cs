using MINIKeyboard.Domain;

namespace MINIKeyboard.Presentation;

/// <summary>A rectangle in a layout's drawing space.</summary>
public readonly record struct LayoutRect(double X, double Y, double Width, double Height)
{
    public double MaxX => X + Width;

    public double MaxY => Y + Height;

    public double MidX => X + Width / 2;

    public double MidY => Y + Height / 2;

    public bool Contains(double x, double y) => x >= X && x <= MaxX && y >= Y && y <= MaxY;
}

/// <summary>Where a layout's controls are drawn, and the keyboard's body around them.</summary>
internal sealed class LayoutGeometry(LayoutRect frame, IReadOnlyDictionary<ControlId, LayoutRect> rects)
{
    // An arrangement is drawn in the same space as the app's layouts: a key 100 wide.
    private const double KeySize = 100;
    private const double KeyGap = 20;
    private const double PressSize = 80;
    private const double TurnSize = 44;
    private const double TurnOffset = 72;
    private const double KnobGap = 30;
    private const double KnobSpacing = 110;
    private const double Margin = 40;

    /// <summary>Room between a knob and its turns, in the app's layouts' space (a key there is 80 or so).</summary>
    internal const double TurnGap = 4;

    public LayoutRect Frame { get; } = frame;

    public IReadOnlyDictionary<ControlId, LayoutRect> Rects { get; } = rects;

    /// <summary>
    /// The cross-platform app's own geometry for its layouts; an arrangement's is drawn from its counts.
    /// </summary>
    public static LayoutGeometry? Of(KeyboardLayout layout) =>
        GridShape.FromId(layout.Id) is { } shape ? Arrangement(shape) : AppGeometry.Named(layout.Id)?.WithEvenTurns();

    /// <summary>
    /// The turns redrawn: all one size, none touching its knob. The original app sizes a knob's turns by the knob (0.6
    /// of it), so a big knob's turns grow with it, and puts some over the edge of their knob. Here every turn is as
    /// large as the layout's smallest — smaller still where one would come closer than <see cref="TurnGap"/> to its
    /// knob — and stands in its original direction from the knob's centre, <see cref="TurnGap"/> off the knob.
    /// </summary>
    public LayoutGeometry WithEvenTurns()
    {
        var turns = new List<(ControlId Control, LayoutRect Rect, LayoutRect Press)>();
        foreach (var (control, rect) in Rects)
        {
            if (control.IsKey || control.Part == KnobPart.Press)
                continue;
            if (Rects.TryGetValue(ControlId.Knob(control.Number, KnobPart.Press), out var press))
                turns.Add((control, rect, press));
        }
        if (turns.Count == 0)
            return this;
        var size = turns.Min(turn =>
            Math.Min(turn.Rect.Width, 2 * (Distance(turn.Rect, turn.Press) - turn.Press.Width / 2 - TurnGap)));
        var rects = new Dictionary<ControlId, LayoutRect>(Rects);
        foreach (var (control, rect, press) in turns)
        {
            var scale = (press.Width / 2 + TurnGap + size / 2) / Distance(rect, press);
            var (x, y) = (press.MidX + (rect.MidX - press.MidX) * scale, press.MidY + (rect.MidY - press.MidY) * scale);
            rects[control] = new LayoutRect(x - size / 2, y - size / 2, size, size);
        }
        return new LayoutGeometry(Frame, rects);
    }

    private static double Distance(LayoutRect a, LayoutRect b) =>
        Math.Sqrt((a.MidX - b.MidX) * (a.MidX - b.MidX) + (a.MidY - b.MidY) * (a.MidY - b.MidY));

    /// <summary>The body and every control.</summary>
    public LayoutRect Bounds()
    {
        var (minX, minY, maxX, maxY) = (Frame.X, Frame.Y, Frame.MaxX, Frame.MaxY);
        foreach (var rect in Rects.Values)
        {
            minX = Math.Min(minX, rect.X);
            minY = Math.Min(minY, rect.Y);
            maxX = Math.Max(maxX, rect.MaxX);
            maxY = Math.Max(maxY, rect.MaxY);
        }
        return new LayoutRect(minX, minY, maxX - minX, maxY - minY);
    }

    /// <summary>
    /// As it looks with the keyboard turned: every rectangle turned about the middle of the bounds. Squares stay
    /// squares; a wide key stands up.
    /// </summary>
    public LayoutGeometry Rotated(Rotation rotation)
    {
        if (rotation == Rotation.Upright)
            return this;
        var bounds = Bounds();
        var (centerX, centerY) = (bounds.MidX, bounds.MidY);
        var sideways = rotation is Rotation.Quarter or Rotation.ThreeQuarters;
        LayoutRect Turn(LayoutRect rect)
        {
            var (dx, dy) = (rect.MidX - centerX, rect.MidY - centerY);
            var (x, y) = rotation switch
            {
                Rotation.Quarter => (-dy, dx),
                Rotation.Half => (-dx, -dy),
                Rotation.ThreeQuarters => (dy, -dx),
                _ => (dx, dy),
            };
            var (width, height) = sideways ? (rect.Height, rect.Width) : (rect.Width, rect.Height);
            return new LayoutRect(centerX + x - width / 2, centerY + y - height / 2, width, height);
        }
        return new LayoutGeometry(Turn(Frame), Rects.ToDictionary(pair => pair.Key, pair => Turn(pair.Value)));
    }

    /// <summary>
    /// Each knob as one body, for drawing the keyboard's shape — its actions are separate controls only for binding.
    /// The body sits on the press action, as large as it; a knob without one sits between its turns, as large as the
    /// layout's other knobs.
    /// </summary>
    public IReadOnlyList<(byte Number, LayoutRect Rect)> KnobOutlines()
    {
        var parts = new SortedDictionary<byte, List<(KnobPart Part, LayoutRect Rect)>>();
        foreach (var (control, rect) in Rects)
        {
            if (control.IsKey)
                continue;
            if (!parts.TryGetValue(control.Number, out var knob))
                parts[control.Number] = knob = [];
            knob.Add((control.Part, rect));
        }
        var pressSize = parts.Values
            .SelectMany(knob => knob)
            .Where(part => part.Part == KnobPart.Press)
            .Select(part => (double?)part.Rect.Width)
            .Max();
        var outlines = new List<(byte, LayoutRect)>();
        foreach (var (number, knob) in parts)
        {
            var press = knob.FindIndex(part => part.Part == KnobPart.Press);
            if (press >= 0)
            {
                outlines.Add((number, knob[press].Rect));
                continue;
            }
            var minX = knob.Min(part => part.Rect.X);
            var maxX = knob.Max(part => part.Rect.MaxX);
            var minY = knob.Min(part => part.Rect.Y);
            var maxY = knob.Max(part => part.Rect.MaxY);
            var size = pressSize ?? Math.Min(maxX - minX, maxY - minY);
            outlines.Add((number, new LayoutRect((minX + maxX - size) / 2, (minY + maxY - size) / 2, size, size)));
        }
        return outlines;
    }

    /// <summary>
    /// An arrangement drawn tidily, since the keyboard's real shape is not known: keys fill the rows from the top left;
    /// knobs stand to the right of the keys — one at their middle, two at the first and last rows (or around the middle
    /// when the rows are too close) — each with its turns either side of its press.
    /// </summary>
    public static LayoutGeometry Arrangement(GridShape shape)
    {
        const double pitch = KeySize + KeyGap;
        int keys = shape.Keys, columns = shape.Columns, knobs = shape.Knobs;
        var rects = new Dictionary<ControlId, LayoutRect>();
        for (var index = 0; index < keys; index++)
        {
            rects[ControlId.Key((byte)(index + 1))] =
                new LayoutRect((index % columns) * pitch, (index / columns) * pitch, KeySize, KeySize);
        }
        var blockWidth = keys == 0 ? 0 : columns * pitch - KeyGap;
        var blockHeight = keys == 0 ? PressSize : shape.Rows * pitch - KeyGap;
        const double knobWidth = 2 * TurnOffset + TurnSize;
        for (var index = 0; index < knobs; index++)
        {
            double centerX, centerY;
            if (keys == 0)
            {
                centerX = knobWidth / 2 + index * (knobWidth + KnobGap);
                centerY = blockHeight / 2;
            }
            else
            {
                var (first, last) = (KeySize / 2, blockHeight - KeySize / 2);
                if (knobs == 1)
                    centerY = blockHeight / 2;
                else if (last - first >= KnobSpacing)
                    centerY = first + (last - first) * index / (knobs - 1);
                else
                    centerY = blockHeight / 2 + (index - (knobs - 1) / 2.0) * KnobSpacing;
                centerX = blockWidth + KnobGap + knobWidth / 2;
            }
            (KnobPart Part, double Offset, double Size)[] parts =
                [(KnobPart.RotateLeft, -TurnOffset, TurnSize), (KnobPart.Press, 0, PressSize),
                    (KnobPart.RotateRight, TurnOffset, TurnSize)];
            foreach (var (part, offset, size) in parts)
            {
                rects[ControlId.Knob((byte)(index + 1), part)] =
                    new LayoutRect(centerX + offset - size / 2, centerY - size / 2, size, size);
            }
        }
        var minX = rects.Values.Min(rect => rect.X);
        var minY = rects.Values.Min(rect => rect.Y);
        var maxX = rects.Values.Max(rect => rect.MaxX);
        var maxY = rects.Values.Max(rect => rect.MaxY);
        var frame = new LayoutRect(minX - Margin, minY - Margin, maxX - minX + 2 * Margin, maxY - minY + 2 * Margin);
        return new LayoutGeometry(frame, rects);
    }
}
