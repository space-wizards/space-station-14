using Content.Shared.Whitelist;

namespace Content.Shared.Silicons.Borgs.Components;

/// <summary>
/// This is used for the core body of a borg. This manages a borg's
/// "brain", legs, modules, and battery. Essentially the master component
/// for borg logic.
/// </summary>
[RegisterComponent]
public sealed class BorgChassisComponent : Component
{
    [DataField("brainEntity")]
    public EntityUid? BrainEntity;

    [DataField("brainWhitelist")]
    public EntityWhitelist? BrainWhitelist;

    public string BrainOrganSlotId = "BorgBrainSlot";
}
