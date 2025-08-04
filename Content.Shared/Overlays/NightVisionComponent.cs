using Content.Shared.Actions;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Overlays;

[RegisterComponent, NetworkedComponent]
public sealed partial class NightVisionComponent : Component
{
    [DataField("color")]
    public string Color;
}
