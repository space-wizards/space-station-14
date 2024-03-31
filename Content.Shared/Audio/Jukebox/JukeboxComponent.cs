using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Jukebox;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedJukeboxSystem))]
[AutoGenerateComponentState]
public sealed partial class JukeboxComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public bool Playing;

    [DataField]
    [AutoNetworkedField]
    public int SelectedSongId;

    [DataField]
    [AutoNetworkedField]
    public float SongStartTime;

    [ViewVariables]
    public EntityUid? AudioStream;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OnState;

    /// <summary>
    /// RSI state for the jukebox being on.
    /// </summary>
    [DataField]
    public string? OffState;

    /// <summary>
    /// RSI state for the jukebox track being selected.
    /// </summary>
    [DataField]
    public string? SelectState;

    [ViewVariables]
    public bool Selecting;

    [ViewVariables]
    public float SelectAccumulator;
}

[Serializable, NetSerializable]
public sealed class JukeboxPlayingMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxStopMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class JukeboxSelectedMessage(int songId) : BoundUserInterfaceMessage
{
    public int SongId { get; } = songId;
}

[Serializable, NetSerializable]
public sealed class JukeboxSetTimeMessage(float songTime) : BoundUserInterfaceMessage
{
    public float SongTime { get; } = songTime;
}

[Serializable, NetSerializable]
public enum JukeboxVisuals : byte
{
    VisualState
}

[Serializable, NetSerializable]
public enum JukeboxVisualState : byte
{
    On,
    Off,
    Select,
}

public enum JukeboxVisualLayers : byte
{
    Base
}
