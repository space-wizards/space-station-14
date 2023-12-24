using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Ghost.Components;

/// <summary>
/// Makes ghosts visible to the entity.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GhostObserverComponent : Component
{
}
