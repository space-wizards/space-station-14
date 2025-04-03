using Content.Shared.Actions;
using Content.Shared.Ninja.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportationActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TeleportationActionEntity;

    /// <summary>
    /// Takes pulled entities with it
    /// </summary>
    [DataField]
    public bool DropsPulled = false;

}
