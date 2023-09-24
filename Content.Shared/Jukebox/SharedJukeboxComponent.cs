using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Jukebox;

[NetworkedComponent, RegisterComponent]
[Access(typeof(SharedJukeboxSystem))]
public partial class JukeboxComponent : Component
{
    [DataField("playing")]
    public bool Playing { get; set; }

    [DataField("selectedSongId")]
    public int SelectedSongID { get; set; }

    [ViewVariables]
    public MusicListDefinition? SelectedSong { get; set; }

    [DataField("songTime")]
    public float SongTime { get; set; }

    [DataField("songStartTime")]
    public float SongStartTime { get; set; }

    [ViewVariables]
    public IPlayingAudioStream? AudioStream { get; set; }

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
}

[Serializable, NetSerializable]
public sealed class JukeboxComponentState : ComponentState
{
    public bool Playing { get; }

    public int SelectedSongID { get; }

    public float SongTime { get; }
    public float SongStartTime { get; }

    public string? MusicCollection { get; }

    public JukeboxComponentState(bool playing, int selectedSongId, float songTime, float songStartTime, string? musicCollection)
    {
        Playing = playing;
        SelectedSongID = selectedSongId;
        SongTime = songTime;
        SongStartTime = songStartTime;
        MusicCollection = musicCollection;
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
