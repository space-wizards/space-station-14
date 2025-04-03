using Robust.Shared.GameStates;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TeleportationActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TeleportationActionEntity;

    /// <summary>
    /// If it leaves pulled entities behind or not
    /// </summary>
    [DataField]
    public bool DropsPulled = false;

}
