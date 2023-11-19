using Robust.Shared.Serialization;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Power.Substation;

[Serializable, NetSerializable]
public enum SubstationIntegrityState
{
    Healthy,	//At 70% or more
    Unhealthy,	// <70% to 30%
    Bad			// <30%
}

[Serializable, NetSerializable]
public enum SubstationVisuals
{
    Screen
}

[RegisterComponent]
public sealed partial class SubstationFuseSlotComponent : Component
{

    [DataField("fuseSlotId", required: true)]
    [ViewVariables(VVAccess.ReadOnly)]
    public string FuseSlotId = string.Empty;

    public bool AllowInsert = true;

}

public sealed class SubstationFuseChangedEvent : EntityEventArgs
{}
