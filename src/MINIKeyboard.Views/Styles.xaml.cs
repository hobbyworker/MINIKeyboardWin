using Microsoft.UI.Xaml;

namespace MINIKeyboard.Views;

/// <summary>The app's styles. The composition root merges them into the application's resources once.</summary>
public sealed partial class Styles : ResourceDictionary
{
    public Styles() => InitializeComponent();

    /// <summary>A style of these, or WinUI's own, by its key.</summary>
    internal static Style Get(string key) => (Style)Application.Current.Resources[key];
}
