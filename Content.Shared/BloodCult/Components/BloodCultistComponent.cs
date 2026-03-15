using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Content.Shared.BloodCult.Prototypes;

namespace Content.Shared.BloodCult;

/// <summary>
/// A Blood Cultist.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(fieldDeltas: true)]
public sealed partial class BloodCultistComponent : Component
{
	/// <summary>
	///		Currently active spells.
	/// </summary>
	[DataField, AutoNetworkedField] public List<ProtoId<CultAbilityPrototype>> KnownSpells = new();

	/// <summary>
	/// 	Amount of decultification effects applied to this cultist.
	///		Will decultify them at 100.
	/// </summary>
	[DataField] public float DeCultification = 0.0f;

	/// <summary>
    ///     Stores if the cultist was revived in the last tick.
    /// </summary>
	[DataField] public bool BeingRevived = false;

	/// <summary>
	///		Studies the veil.
	/// </summary>
	[DataField] public bool StudyingVeil = false;

	/// <summary>
	///		Is Nar'Sie being summoned?
	/// </summary>
	[DataField] public EntityCoordinates? NarsieSummoned = null;

	/// <summary>
	///		Did this cultist just fail to summon Nar'Sie?
	/// </summary>
	[DataField] public bool FailedNarsieSummon = false;

	/// <summary>
	///		Show the tear veil rune?
	/// </summary>
	[DataField, AutoNetworkedField] public bool ShowTearVeilRune = false;

	/// <summary>
	///		One of the three locations the tear veil ritual can happen at
	/// <summary>
	[DataField] public WeakVeilLocation? LocationForSummon = null;

	/// <summary>
	///		Message the cultist is attempting to commune to the others.
	/// </summary>
	[DataField] public string? CommuningMessage = null;

	/// <summary>
	/// The Uid of the person trying to revive the cultist.
	/// </summary>
	[DataField] public EntityUid? ReviverUid = null;

	/// <summary>
	/// The original blood reagent before becoming a cultist.
	/// Used to restore the blood type when deconverted.
	/// </summary>
	[DataField] public string OriginalBloodReagent = "Blood";

	/// <summary>
	/// Reagent used when applying cult blood (e.g. Unholy Blood). Set from BloodCultRule on the server.
	/// </summary>
	[DataField] public string CultBloodReagent = "UnholyBlood";

	/// <summary>
	/// Used as part of runes that require a sacrifice.
	/// </summary>
	[DataField] public SacrificingData? Sacrifice = null;

	/// <summary>
	/// Used as part of runes that cause a conversion.
	/// </summary>
	[DataField] public ConvertingData? Convert = null;

	/// <summary>
	/// The list of sacrifice targets.
	/// </summary>
	[DataField] public List<EntityUid> Targets = new List<EntityUid>();

	public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultFaction";

}

/// <summary>
///	Contains information about a place where the veil is weak.
/// </summary>
public struct WeakVeilLocation
{
	public readonly string Name;
	public readonly EntityUid Uid;
	public readonly string ProtoUid;
	public readonly EntityCoordinates Coordinates;
	public readonly float ValidRadius;

	public WeakVeilLocation(string name, EntityUid uid, string protoUid, EntityCoordinates coordinates, float validRadius)
	{
		Name = name;
		Uid = uid;
		ProtoUid = protoUid;
		Coordinates = coordinates;
		ValidRadius = validRadius;
	}
}

public struct SacrificingData
{
	public EntityUid Victim;
	public EntityUid[] Invokers;

	public SacrificingData(EntityUid victim, EntityUid[] invokers)
	{
		Victim = victim;
		Invokers = invokers;
	}
}

public struct ConvertingData
{
	public EntityUid Subject;
	public EntityUid[] Invokers;

	public ConvertingData(EntityUid subject, EntityUid[] invokers)
	{
		Subject = subject;
		Invokers = invokers;
	}
}

[Serializable, NetSerializable]
public enum CultHaloVisuals
{
	CultHalo,
}

[Serializable, NetSerializable]
public enum CultEyesVisuals
{
	CultEyes,
}
