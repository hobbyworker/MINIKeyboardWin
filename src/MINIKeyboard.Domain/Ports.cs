namespace MINIKeyboard.Domain;

// What the outer layers provide: the keyboard, its connection, the device log and the user's choices.
//
// The use cases run on the interface's thread. Work that waits — on the keyboard, or for time to pass — is a task:
// whoever implements the port does the waiting elsewhere, and the caller's synchronization context brings the result
// back. A failed transfer throws a KeyboardDeviceException.

/// <summary>Finding the keyboard and keeping its configuration interface open.</summary>
public interface IKeyboardConnectionRepository
{
    /// <summary>The current state, then every change.</summary>
    IAsyncEnumerable<ConnectionState> ConnectionStates(CancellationToken cancellation = default);

    /// <summary>Looks for the keyboard again — after another app let go of it.</summary>
    void Retry();
}

/// <summary>Talking to the open keyboard.</summary>
public interface IKeyboardDeviceRepository
{
    /// <summary>Asks a 884x keyboard who it is (<c>FB FB FB</c>) once. Null when no answer comes in time.</summary>
    Task<KeyboardIdentity?> AskIdentityAsync(CancellationToken cancellation = default);

    /// <summary>The actions one layer holds, by control, for the controls of <paramref name="layout"/> — standard and legacy formats only.</summary>
    Task<IReadOnlyDictionary<ControlId, KeyAction>> ReadActionsAsync(
        int layer, RecordFormat format, KeyboardLayout layout, CancellationToken cancellation = default);

    /// <summary>One layer's lights — the standard format only.</summary>
    Task<LayerLighting> ReadLightingAsync(int layer, CancellationToken cancellation = default);

    /// <summary>
    /// Writes every changed action and light of <paramref name="configuration"/>, in the order the keyboard's vendor tool
    /// writes them, and has the keyboard keep them.
    /// </summary>
    Task WriteAsync(
        KeyboardConfiguration configuration, CommandSet commands, KeyboardLayout layout,
        CancellationToken cancellation = default);
}

/// <summary>The layouts users chose, by keyboard.</summary>
public interface ILayoutProfileRepository
{
    LayoutProfile? Profile(UsbIdentity keyboard);

    void Save(LayoutProfile profile, UsbIdentity keyboard);
}

/// <summary>The reports exchanged with the keyboard, for the device log.</summary>
public interface IDeviceLogRepository
{
    /// <summary>The current entries, then the whole list after changes.</summary>
    IAsyncEnumerable<IReadOnlyList<DeviceLogEntry>> Entries(CancellationToken cancellation = default);

    void Clear();
}

/// <summary>Choosing between the keyboard on USB and a simulated one.</summary>
public interface IKeyboardSourceRepository
{
    KeyboardSource Current { get; }

    void Select(KeyboardSource source);
}
