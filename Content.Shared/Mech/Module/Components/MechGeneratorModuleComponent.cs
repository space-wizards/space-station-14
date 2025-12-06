using Robust.Shared.Serialization;

namespace Content.Shared.Mech.Components;

/// <summary>
/// Defines the type of energy generation provided by a mech generator module.
/// </summary>
[Serializable, NetSerializable]
public enum MechGenerationType
{
	TeslaRelay,
	FuelGenerator
}

/// <summary>
/// Unified configuration for mech generator modules. Controls how the module supplies.
/// </summary>
[RegisterComponent]
public sealed partial class MechGeneratorModuleComponent : Component
{
	/// <summary>
	/// Selects the generator mode.
	/// </summary>
	[DataField]
	public MechGenerationType GenerationType;

	/// <summary>
	/// Tesla-specific configuration.
	/// </summary>
	[DataField]
	public TeslaRelayGeneratorConfig? Tesla;
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class TeslaRelayGeneratorConfig
{
	[DataField]
	public float ChargeRate = 20f;

	[DataField]
	public float Radius = 3f;
}
