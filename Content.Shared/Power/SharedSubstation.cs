using Robust.Shared.Serialization;
using Content.Shared.Containers.ItemSlots;

namespace Content.Shared.Power;

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