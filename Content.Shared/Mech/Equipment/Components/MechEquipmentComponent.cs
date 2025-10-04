using Content.Shared.DoAfter;
using Content.Shared.Mech.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="MechComponent"/>
/// </summary>
[RegisterComponent]
public sealed partial class MechEquipmentComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment.
    /// </summary>
    [DataField]
    public float InstallDuration = 5;

    /// <summary>
    /// Space units this equipment occupies in the mech (for UI display).
    /// </summary>
    [DataField]
    public int Size = 1;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables]
    public EntityUid? EquipmentOwner;

    /// <summary>
    /// If true, this equipment cannot be used outside of a mech.
    /// </summary>
    [DataField]
    public bool BlockUseOutsideMech = true;
}

/// <summary>
/// Raised on the equipment when the installation is finished successfully.
/// </summary>
public sealed class MechEquipmentInstallFinished(EntityUid mech) : EntityEventArgs
{
    public EntityUid Mech = mech;
}

/// <summary>
/// Raised on the equipment when the installation fails.
/// </summary>
public sealed class MechEquipmentInstallCancelled : EntityEventArgs
{
}

[Serializable, NetSerializable]
public sealed partial class GrabberDoAfterEvent : SimpleDoAfterEvent
{
}

[Serializable, NetSerializable]
public sealed partial class InsertEquipmentEvent : SimpleDoAfterEvent
{
}
