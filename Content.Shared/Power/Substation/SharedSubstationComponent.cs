using Robust.Shared.Serialization;

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