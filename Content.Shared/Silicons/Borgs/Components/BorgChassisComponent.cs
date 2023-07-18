using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

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

    [DataField("borgJobId", customTypeSerializer: typeof(PrototypeIdSerializer<JobPrototype>))]
    public string BorgJobId = "Borg";
}
