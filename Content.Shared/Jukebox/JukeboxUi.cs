using Robust.Shared.Serialization;

namespace Content.Shared.Jukebox;


[Serializable, NetSerializable]
public enum JukeboxUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class JukeboxBoundUserInterfaceState : BoundUserInterfaceState
{
    public bool Playing { get; }
    public int SelectedSongID { get; }
    public JukeboxBoundUserInterfaceState(bool playing, int selectedSongID)
    {
        Playing = playing;
        SelectedSongID = selectedSongID;
    }
}
