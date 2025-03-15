using Content.Shared.Clothing.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Clothing.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(WeldingMaskSystem))]
public sealed partial class WeldingMaskComponent : Component
{
}
