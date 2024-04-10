using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Audio.Jukebox;


[Serializable, NetSerializable]
public enum JukeboxUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class JukeboxBoundInterfaceState : BoundUserInterfaceState
{
    public NetEntity? Audio;
    public ProtoId<JukeboxPrototype>? SelectedSong;
}
