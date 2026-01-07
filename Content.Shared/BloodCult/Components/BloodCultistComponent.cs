// SPDX-FileCopyrightText: 2025 Skye <57879983+Rainbeon@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Terkala <appleorange64@gmail.com>
// SPDX-FileCopyrightText: 2025 kbarkevich <24629810+kbarkevich@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 taydeo <td12233a@gmail.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later OR MIT

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
    ///     Stores captured blood.
    /// </summary>
    [DataField] public int Blood = 0;

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
	///		The selected place that Nar'Sie will be summoned.
	/// <summary>
	[DataField] public WeakVeilLocation? LocationForSummon = null;

	[DataField] public bool ConfirmedSummonLocation = false;
	[DataField] public bool AskedToConfirm = false;
	[DataField] public bool TryingDrawTearVeil = false;

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

	[DataField] public SacrificingData? Sacrifice = null;
	[DataField] public ConvertingData? Convert = null;

	/// <summary>
	/// The list of sacrifice targets.
	/// </summary>
	[DataField] public List<EntityUid> Targets = new List<EntityUid>();

	public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "BloodCultFaction";
/*
    #region Prototypes

    //[DataField] public List<ProtoId<HereticKnowledgePrototype>> BaseKnowledge = new()
    //{
    //    "BreakOfDawn",
    //    "HeartbeatOfMansus",
    //    "AmberFocus",
    //    "LivingHeart",
    //    "CodexCicatrix",
    //};

	//[DataField] public List<ProtoId<BloodCultSpellPrototype>> CurrentSpells = new()
	//{
	//};

    #endregion

    [DataField, AutoNetworkedField] public List<ProtoId<HereticRitualPrototype>> KnownRituals = new();
    [DataField] public ProtoId<HereticRitualPrototype>? ChosenRitual;

    /// <summary>
    ///     Contains the list of targets that are eligible for sacrifice.
    /// </summary>
    [DataField, AutoNetworkedField] public List<NetEntity?> SacrificeTargets = new();

    /// <summary>
    ///     How much targets can a heretic have?
    /// </summary>
    [DataField, AutoNetworkedField] public int MaxTargets = 5;

    // hardcoded paths because i hate it
    // "Ash", "Lock", "Flesh", "Void", "Blade", "Rust"
    /// <summary>
    ///     Indicates a path the heretic is on.
    /// </summary>
    [DataField, AutoNetworkedField] public string? CurrentPath = null;

    /// <summary>
    ///     Indicates a stage of a path the heretic is on. 0 is no path, 10 is ascension
    /// </summary>
    [DataField, AutoNetworkedField] public int PathStage = 0;

    [DataField, AutoNetworkedField] public bool Ascended = false;

    /// <summary>
    ///     Used to prevent double casting mansus grasp.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)] public bool MansusGraspActive = false;

    /// <summary>
    ///     Indicates if a heretic is able to cast advanced spells.
    ///     Requires wearing focus, codex cicatrix, hood or anything else that allows him to do so.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)] public bool CanCastSpells = false;

    /// <summary>
    ///     dunno how to word this
    ///     its for making sure the next point update is 20 minutes in
    /// </summary>
    [DataField, AutoNetworkedField] public TimeSpan NextPointUpdate;

    /// <summary>
    ///     dunno how to word this
    ///     its for making sure the next point update is 20 minutes in
    /// </summary>
    [DataField, AutoNetworkedField] public TimeSpan PointCooldown = TimeSpan.FromMinutes(20);

    /// <summary>
    ///     when the time delta alert happens
    /// </summary>
    [DataField, AutoNetworkedField] public TimeSpan AlertTime;

    /// <summary>
    ///     how long 2 wait
    /// </summary>
    [DataField, AutoNetworkedField] public TimeSpan AlertWaitTime = TimeSpan.FromSeconds(10);
	*/
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
