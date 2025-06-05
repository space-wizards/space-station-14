using Content.Shared.Movement.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Movement.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedMoverController))]
public sealed partial class MovementRelayTargetComponent : Component
{
    /// <summary>
    /// The entity that is relaying to this entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid Source;
}
