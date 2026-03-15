using Robust.Shared.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Server.GameTicking.Rules;
using Content.Shared.Mind;
using Content.Shared.BloodCult;
using Content.Shared.BloodCult.Components;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the BloodCultRuleSystem that stores info about winning/losing, player counts required
///	for stuff, and other round-wide stuff.
/// </summary>
// Rift system used to set up final summoning ritual site, to be added later
//[RegisterComponent, Access(typeof(BloodCultRuleSystem), typeof(BloodCult.EntitySystems.BloodCultRiftSetupSystem))]
[RegisterComponent, Access(typeof(BloodCultRuleSystem))]//Swap for the above when the rift is added
public sealed partial class BloodCultRuleComponent : Component
{
	/// <summary>
	///	Possible Nar'Sie summon locations.
	/// </summary>
	[DataField(required: true)]
	/// Overridden by the roundstart.yml setting, listed here just in case.
	public string PossibleVeilBeaconPrefix = "DefaultStationBeacon";

	/// <summary>
	/// Reagent prototype ID used as "cult blood" (e.g. Unholy Blood for bleeding, ritual tracking, Edge Essentia).
	/// Overridden by the roundstart.yml setting.
	/// </summary>
	[DataField]
	public string CultBloodReagent = "UnholyBlood";

	[DataField] public WeakVeilLocation? WeakVeil1 = null;
	[DataField] public WeakVeilLocation? WeakVeil2 = null;
	[DataField] public WeakVeilLocation? WeakVeil3 = null;

	/// <summary>
	///		Stores the location the existing cultists have decided to summon Nar'Sie.
	/// </summary>
	[DataField] public WeakVeilLocation? LocationForSummon = null;

	/// <summary>
	/// Total sacrifices made.
	/// </summary>
	[DataField] public int TotalSacrifices = 0;

	/// <summary>
	/// Total conversions made throughout the round.
	/// </summary>
	[DataField] public int TotalConversions = 0;

	/// <summary>
	/// Current amount of blood collected for the ritual.
	/// </summary>
	[DataField] public double BloodCollected = 0.0;

	/// <summary>
	/// Blood required to reach the first phase (Eyes).
	/// </summary>
	[DataField] public double BloodRequiredForEyes = 0.0;

	/// <summary>
	/// Blood required to reach the second phase (Rise).
	/// </summary>
	[DataField] public double BloodRequiredForRise = 0.0;

	/// <summary>
	/// Blood required to reach the third phase (Veil Weakened).
	/// </summary>
	[DataField] public double BloodRequiredForVeil = 0.0;

	/// <summary>
	///	Conversions needed until glowing eyes -- set when cult is initialized.
	/// </summary>
	[DataField] public int ConversionsUntilEyes = 0;

	/// <summary>
	///	Conversions needed until rise -- set when cult is initialized.
	/// </summary>
	[DataField] public int ConversionsUntilRise = 0;

	/// <summary>
	///	Has the cult gained glowing eyes yet?
	/// </summary>
	[DataField] public bool HasEyes = false;

	/// <summary>
	///	Has the cult risen yet?
	/// </summary>
	[DataField] public bool HasRisen = false;

	/// <summary>
	/// Nar'Sie ready to summon.
	/// </summary>
	[DataField] public bool VeilWeakened = false;

	/// <summary>
	/// Has the blood anomaly spawn been scheduled after weakening the veil?
	/// </summary>
	[DataField] public bool BloodAnomalySpawnScheduled = false;

	/// <summary>
	/// Has the blood anomaly been spawned for the final ritual?
	/// </summary>
	[DataField] public bool BloodAnomalySpawned = false;

	/// <summary>
	/// The time the blood anomaly should be spawned, if scheduled.
	/// </summary>
	[DataField] public TimeSpan? BloodAnomalySpawnTime = null;

	/// <summary>
	/// The spawned blood anomaly entity.
	/// </summary>
	[DataField] public EntityUid? BloodAnomalyUid = null;

	/// <summary>
	/// Whether or not the VeilWeakened announcement has played.
	/// </summary>
	[DataField] public bool VeilWeakenedAnnouncementPlayed = false;

	/// <summary>
	///	Have the cultists won?
	/// </summary>
	[DataField] public bool CultistsWin = false;

	[DataField] public TimeSpan? CultVictoryEndTime = null;
	[DataField] public bool CultVictoryAnnouncementPlayed = false;

	/// <summary>
	///	Time in seconds after Nar'Sie spawns for the shuttle to be called.
	/// </summary>
	[DataField] public TimeSpan CultVictoryEndDelay = TimeSpan.FromSeconds(15);

	/// <summary>
	/// Time after the evac shuttle is dispatched for it to arrive.
	/// </summary>
	[DataField] public TimeSpan ShuttleCallTime = TimeSpan.FromMinutes(2);

	// <summary>
	/// When to give initial report on cultist count and crew count.
	/// </summary>
	[DataField] public TimeSpan? InitialReportTime = null;

	/// <summary>
	/// Number of cultists required to sacrifice a dead player.
	/// </summary>
	[DataField] public int CultistsToSacrifice = 1;

	/// <summary>
	/// Number of players required to convert a player.
	/// </summary>
	[DataField] public int CultistsToConvert = 2;

	/// <summary>
	/// Minimum number of cultists required on Tear Veil runes to complete the ritual.
	/// Calculated at round start based on player count (1/8th of total players, minimum 3).
	/// </summary>
	[DataField] public int MinimumCultistsForVeilRitual = 3;
}
