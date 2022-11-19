using System.Threading;

namespace Content.Shared.Mech.Equipment.Components;

[RegisterComponent]
public sealed class MechEquipmentComponent : Component
{
    /// <summary>
    /// How long does it take to install this piece of equipment
    /// </summary>
    [DataField("installDuration")]
    public float InstallDuration = 5;

    public CancellationTokenSource? TokenSource = null;
}

public sealed class MechEquipmentInstallFinished : EntityEventArgs
{
    public EntityUid Mech;

    public MechEquipmentInstallFinished(EntityUid mech)
    {
        Mech = mech;
    }
}

public sealed class MechEquipmentInstallCancelled : EntityEventArgs
{
}
