using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Jukebox;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedJukeboxSystem))]
[AutoGenerateComponentState]
public partial class JukeboxComponent : Component
{
    [DataField("playing")]
    [AutoNetworkedField]
    public bool Playing;

    [DataField("selectedSongId")]
    [AutoNetworkedField]
    public int SelectedSongID;

    [ViewVariables]
    public MusicListDefinition? SelectedSong;

    [DataField("songTime")]
    [AutoNetworkedField]
    public float SongTime;

    [DataField("songStartTime")]
    [AutoNetworkedField]
    public float SongStartTime;

    [ViewVariables]
    public IPlayingAudioStream? AudioStream;

    [ValidatePrototypeId<MusicListPrototype>]
    public string MusicCollection = "MusicStandard";

    [ViewVariables]
    public MusicListPrototype JukeboxMusicCollection = default!;

    [DataField("onState")]
    public string? OnState;

    [DataField("offState")]
    public string? OffState;

    [DataField("selectState")]
    public string? SelectState;

    [ViewVariables]
    public bool Selecting;

    [ViewVariables]
    public float SelectAccumulator;
}

[Serializable, NetSerializable]
public sealed class JukeboxPlayingMessage : BoundUserInterfaceMessage
{
    public JukeboxPlayingMessage()
    {

    }
}

[Serializable, NetSerializable]
public sealed class JukeboxStopMessage : BoundUserInterfaceMessage
{
    public JukeboxStopMessage()
    {

    }
}

[Serializable, NetSerializable]
public sealed class JukeboxSelectedMessage : BoundUserInterfaceMessage
{
    public int Songid { get; }
    public JukeboxSelectedMessage(int songid)
    {
        Songid = songid;
    }
}

[Serializable, NetSerializable]
public sealed class JukeboxSetTimeMessage : BoundUserInterfaceMessage
{
    public float SongTime { get; }
    public JukeboxSetTimeMessage(float songTime)
    {
        SongTime = songTime;
    }
}

[Serializable, NetSerializable]
public enum JukeboxVisuals
{
    VisualState
}

[Serializable, NetSerializable]
public enum JukeboxVisualState
{
    On,
    Off,
    Select,
}

public enum JukeboxVisualLayers : byte
{
    Base
}
