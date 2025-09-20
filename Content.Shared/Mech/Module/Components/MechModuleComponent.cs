using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared.Mech.Components;

[RegisterComponent]
public sealed partial class MechModuleComponent : Component
{
    /// <summary>
    /// How long it takes to install this passive module.
    /// </summary>
    [DataField]
    public float InstallDuration = 5f;

    /// <summary>
    /// Space units this module occupies in the mech (for UI display).
    /// </summary>
    [DataField]
    public int Size = 1;

    /// <summary>
    /// The mech that the module is inside of.
    /// </summary>
    [ViewVariables]
    public EntityUid? ModuleOwner;
}

/// <summary>
/// Raised on the module when the installation is finished successfully.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class InsertModuleEvent : SimpleDoAfterEvent
{
}
