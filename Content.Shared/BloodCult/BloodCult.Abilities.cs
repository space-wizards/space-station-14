using Robust.Shared.Serialization;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;
using Content.Shared.BloodCult.Prototypes;
using Content.Shared.Actions;
using Content.Shared.DoAfter;

namespace Content.Shared.BloodCult;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultistSpellComponent : Component
{
	public static List<ProtoId<CultAbilityPrototype>> ValidSpells = new List<ProtoId<CultAbilityPrototype>>{"SummonDagger", "SanguineDream", "CultTwistedConstruction"};

	/// <summary>
	/// 	ID of the prototype that summons this spell.
	///		(I hate that there doesn't seem to be a way to fetch this in another way...)
	/// </summary>
	[DataField] public ProtoId<CultAbilityPrototype> AbilityId = default!;

	/// <summary>
	/// 	Specifies the number of charges remaining.
	/// </summary>
	[DataField] public uint Charges = 1;

	/// <summary>
	///		Specifies if the spell has infinite charges.
	/// </summary>
	[DataField] public bool Infinite = false;

	/// <summary>
	/// 	Specifies the health cost of the spell.
	/// </summary>
	[DataField] public int HealthCost = 0;

	/// <summary>
	/// 	Specifies the recharge time in seconds
	/// </summary>
	[DataField] public int RechargeTime = 1;

	/// <summary>
	///		The verbal invocation when used.
	/// </summary>
	[DataField] public string Invocation = "";

	/// <summary>
	///		The sound to play when cast.
	/// </summary>
	[DataField] public SoundSpecifier? CastSound = null;
}

#region DoAfters

[Serializable, NetSerializable] public sealed partial class DrawRuneDoAfterEvent : SimpleDoAfterEvent
{
	public NetEntity NetCarverUid;
	public NetEntity NetRune;
	public NetCoordinates NetCoords;
	public string EntityId = string.Empty;
	public int BleedOnCarve;
	public SoundSpecifier? CarveSound;
	public uint? EffectId;
	public TimeSpan Duration;

	public DrawRuneDoAfterEvent(NetEntity carverUid, NetEntity rune, NetCoordinates coords, string entityId, int bleedOnCarve, SoundSpecifier? carveSound, uint? effectId, TimeSpan duration)
	{
		NetCarverUid = carverUid;
		NetRune = rune;
		NetCoords = coords;
		EntityId = entityId;
		BleedOnCarve = bleedOnCarve;
		CarveSound = carveSound;
		EffectId = effectId;
		Duration = duration;
	}

	public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable] public sealed partial class CarveSpellDoAfterEvent : SimpleDoAfterEvent
{
	public NetEntity NetCarverUid;
	public ProtoId<CultAbilityPrototype> CultAbilityId;
	public bool RecordKnownSpell;
	//When runes are re-added, uncomment this
	//public bool StandingOnRune;

	//When runes are re-added, uncomment this and swap with below
	//public CarveSpellDoAfterEvent(NetEntity carverUid, ProtoId<CultAbilityPrototype> cultAbilityId, bool recordKnownSpell, bool standingOnRune)
	public CarveSpellDoAfterEvent(NetEntity carverUid, ProtoId<CultAbilityPrototype> cultAbilityId, bool recordKnownSpell)
	{
		NetCarverUid = carverUid;
		CultAbilityId = cultAbilityId;
		RecordKnownSpell = recordKnownSpell;
		//When runes are re-added, uncomment this
		//StandingOnRune = standingOnRune;
	}

	public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable] public sealed partial class TwistedConstructionDoAfterEvent : SimpleDoAfterEvent
{
	public NetEntity NetTarget;

	public TwistedConstructionDoAfterEvent(NetEntity target)
	{
		NetTarget = target;
	}

	public override DoAfterEvent Clone() => this;
}

[Serializable, NetSerializable] public sealed partial class MindshieldBreakDoAfterEvent : SimpleDoAfterEvent
{
	public NetEntity NetVictim;
	public NetEntity[] NetParticipants = Array.Empty<NetEntity>();
	public NetCoordinates NetRuneLocation;

	public MindshieldBreakDoAfterEvent(NetEntity victim, NetEntity[] participants, NetCoordinates runeLocation)
	{
		NetVictim = victim;
		NetParticipants = participants;
		NetRuneLocation = runeLocation;
	}

	public override DoAfterEvent Clone() => this;
}

#endregion

#region Spells

[Serializable, NetSerializable]
public sealed partial class SpellsMessage : BoundUserInterfaceMessage
{
	public ProtoId<CultAbilityPrototype> ProtoId;

	public SpellsMessage(ProtoId<CultAbilityPrototype> protoId)
	{
		ProtoId = protoId;
	}
}
[Serializable, NetSerializable]
public enum SpellsUiKey : byte
{
	Key
}

[Serializable, NetSerializable]
public sealed class BloodCultSpellsBuiState : BoundUserInterfaceState
{
	public BloodCultSpellsBuiState()
	{}
}

public sealed partial class EventCultistStudyVeil : InstantActionEvent { }
public sealed partial class EventCultistSummonDagger : InstantActionEvent { }
public sealed partial class EventCultistSanguineDream : EntityTargetActionEvent { }
public sealed partial class EventCultistTwistedConstruction : EntityTargetActionEvent { }

#endregion

[Serializable, NetSerializable]
public sealed class RuneDrawingEffectEvent : EntityEventArgs
{
    public readonly uint EffectId;
    public readonly string? Prototype;
    public readonly NetCoordinates Coordinates;
    public readonly RuneEffectAction Action;
    public readonly TimeSpan Duration;

    public RuneDrawingEffectEvent(uint effectId, string? prototype, NetCoordinates coordinates, RuneEffectAction action, TimeSpan duration)
    {
        EffectId = effectId;
        Prototype = prototype;
        Coordinates = coordinates;
        Action = action;
        Duration = duration;
    }
}

[Serializable, NetSerializable]
public enum RuneEffectAction : byte
{
    Start,
    Stop
}
