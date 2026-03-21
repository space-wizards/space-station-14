using Content.Shared.Carrying.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Carrying.Components;

/// <summary>
/// Indicates this entity can be fireman carried.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCarryingSystem))]
public sealed partial class CarriableComponent : Component
{
    [AutoNetworkedField, DataField]
    public EntityUid? CarriedBy;
}
