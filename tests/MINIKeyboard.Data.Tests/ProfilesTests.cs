using MINIKeyboard.Domain;

namespace MINIKeyboard.Data.Tests;

/// <summary>The layouts file keeps the shape the other editions write.</summary>
public sealed class ProfilesTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"minikeyboard-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(directory, "layouts.json");

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void LayoutsAreKeptByKeyboard()
    {
        var profiles = new FileLayoutProfiles(FilePath);
        Assert.Null(profiles.Profile(KeyboardFamilies.K8890Usb));
        profiles.Save(new LayoutProfile("6add1", Rotation.Quarter), new UsbIdentity(0x514C, 0x8850));
        profiles.Save(new LayoutProfile("grid-6-3-1", Rotation.Half), KeyboardFamilies.K8890Usb);

        var again = new FileLayoutProfiles(FilePath);
        Assert.Equal(new LayoutProfile("grid-6-3-1", Rotation.Half), again.Profile(KeyboardFamilies.K8890Usb));
        Assert.Equal(new LayoutProfile("6add1", Rotation.Quarter), again.Profile(new UsbIdentity(0x514C, 0x8850)));
        Assert.Equal(
            """
            {
              "1189:8890": {
                "layout": "grid-6-3-1",
                "rotation": 2
              },
              "514C:8850": {
                "layout": "6add1",
                "rotation": 1
              }
            }
            """.ReplaceLineEndings("\n"),
            File.ReadAllText(FilePath));
        Assert.False(File.Exists(Path.Combine(directory, "layouts.json.partial")));
    }

    [Fact]
    public void AFileThatCannotBeReadIsEmpty()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, """{"1189:8890": {"layout": "grid-6-3-1"}}""");
        Assert.Null(new FileLayoutProfiles(FilePath).Profile(KeyboardFamilies.K8890Usb));
        File.WriteAllText(FilePath, """{"1189:8890": {"layout": "grid-6-3-1", "rotation": 300}}""");
        Assert.Null(new FileLayoutProfiles(FilePath).Profile(KeyboardFamilies.K8890Usb));
        File.WriteAllText(FilePath, "not json");
        Assert.Null(new FileLayoutProfiles(FilePath).Profile(KeyboardFamilies.K8890Usb));
        // An extra field is passed over; a quarter turn too many wraps.
        File.WriteAllText(FilePath, """{"1189:8890": {"layout": "grid-6-3-1", "rotation": 5, "note": 1}}""");
        Assert.Equal(
            new LayoutProfile("grid-6-3-1", Rotation.Quarter),
            new FileLayoutProfiles(FilePath).Profile(KeyboardFamilies.K8890Usb));
    }
}
