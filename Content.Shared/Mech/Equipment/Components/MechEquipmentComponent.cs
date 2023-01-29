using System.Threading;
using Content.Shared.Mech.Components;

namespace Content.Shared.Mech.Equipment.Components;

/// <summary>
/// A piece of equipment that can be installed into <see cref="SharedMechComponent"/>
/// </summary>
[RegisterComponent]
public sealed class MechEquipmentComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment
    /// </summary>
    [DataField("installDuration")]
    public float InstallDuration = 5;

    /// <summary>
    /// The mech that the equipment is inside of.
    /// </summary>
    [ViewVariables]
    public EntityUid? EquipmentOwner;

    public CancellationTokenSource? TokenSource = null;
}

/// <summary>
/// Raised on the equipment when the installation is finished successfully
/// </summary>
public sealed class MechEquipmentInstallFinished : EntityEventArgs
{
    public EntityUid Mech;

    public MechEquipmentInstallFinished(EntityUid mech)
    {
        Mech = mech;
    }
}

/// <summary>
/// Raised on the equipment when the installation fails.
/// </summary>
public sealed class MechEquipmentInstallCancelled : EntityEventArgs
{
}
