
using Robust.Shared.GameStates;

namespace Content.Shared.Magic.Components;

// On all variants of the stone statue: petrified or animate.
[RegisterComponent, NetworkedComponent]
public sealed partial class StoneStatueComponent : Component
{
    // TODO: String/ShaderInstance DataField for other similar purposes (e.g. gold shader for Midas touch)
}
