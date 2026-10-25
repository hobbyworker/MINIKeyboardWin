namespace MINIKeyboard.Presentation;

public readonly partial record struct Rgb
{
    /// <summary>
    /// The 56 swatches of the cross-platform app's RGB page (<c>LED_color_1</c> … <c>LED_color_56</c>).
    /// </summary>
    public static IReadOnlyList<Rgb> Palette { get; } =
    [
        new(255, 0, 0), new(255, 128, 60), new(255, 255, 60), new(0, 255, 0), new(0, 255, 255), new(0, 0, 255),
        new(128, 0, 128), new(139, 0, 0), new(255, 165, 0), new(255, 255, 150), new(125, 255, 0), new(0, 139, 139),
        new(0, 0, 139), new(255, 0, 255), new(128, 0, 0), new(255, 140, 0), new(255, 215, 0), new(0, 255, 128),
        new(224, 255, 255), new(135, 206, 235), new(75, 0, 130), new(255, 102, 102), new(255, 200, 100),
        new(255, 250, 100), new(0, 128, 0), new(0, 127, 150), new(0, 128, 128), new(230, 230, 250),
        new(255, 192, 203), new(255, 200, 0), new(205, 170, 0), new(128, 128, 0), new(0, 255, 127), new(75, 0, 130),
        new(143, 0, 255), new(255, 69, 0), new(255, 85, 0), new(200, 255, 0), new(128, 255, 128), new(128, 192, 192),
        new(173, 216, 230), new(128, 128, 192), new(255, 255, 150), new(255, 120, 0), new(150, 150, 100),
        new(34, 139, 34), new(0, 100, 255), new(0, 127, 255), new(224, 176, 255), new(255, 0, 255), new(255, 102, 0),
        new(150, 190, 100), new(57, 255, 20), new(0, 200, 100), new(128, 128, 192), new(200, 200, 150),
    ];
}
