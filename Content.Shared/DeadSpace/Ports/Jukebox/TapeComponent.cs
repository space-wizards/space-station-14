using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.DeadSpace.Ports.Jukebox;

[RegisterComponent, NetworkedComponent]
public sealed partial class TapeComponent : Component
{
    [DataField("songs")]
    public List<JukeboxSong> Songs { get; set; } = new();
}

[Serializable, NetSerializable]
public sealed partial class TapeComponentState : ComponentState
{
    public List<JukeboxSong> Songs { get; set; } = new();
}
