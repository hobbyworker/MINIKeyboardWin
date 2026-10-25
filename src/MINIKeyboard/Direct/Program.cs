using Velopack;

namespace MINIKeyboard.Direct;

/// <summary>
/// The direct download's entry point, in place of the one WinUI writes: Velopack first — Setup, an update and the
/// uninstaller start the app with arguments of their own, and it does what they ask and exits — then the app.
/// </summary>
internal static class Program
{
    [STAThread]
    private static void Main()
    {
        VelopackApp.Build().Run();
        XamlGeneratedProgram.XamlGeneratedMain();
    }
}
