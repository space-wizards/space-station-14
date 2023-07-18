using Content.Shared.Whitelist;
using Robust.Shared.Containers;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent]
public sealed class BorgChassisComponent : Component
{
    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    [DataField("brainContainerId")]
    public string BrainContainerId = "borg_brain";

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot BrainContainer = default!;

    public EntityUid? BrainEntity => BrainContainer.ContainedEntity;
}
