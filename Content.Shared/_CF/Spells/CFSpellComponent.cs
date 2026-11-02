using Content.Shared.Actions;
using Content.Shared.CF.Character;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CF.Spells;

/// <summary>
/// Data for one CarpFinder spell action. The radial wheel casts it.
/// Area sizes are in feet on the real map grid (one tile is 5 feet), not melee reach.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CFSpellComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<CFSpellPrototype> Spell;

    /// <summary>0 is a cantrip and does not spend a slot.</summary>
    [DataField, AutoNetworkedField]
    public int Rank;

    [DataField, AutoNetworkedField]
    public CFSpellDelivery Delivery = CFSpellDelivery.Target;

    [DataField, AutoNetworkedField]
    public CFSpellAreaKind Area = CFSpellAreaKind.None;

    [DataField, AutoNetworkedField]
    public int AreaFeet;

    /// <summary>0 means the effect comes from the caster (cone, line, emanation, or self).</summary>
    [DataField, AutoNetworkedField]
    public int RangeFeet;

    [DataField, AutoNetworkedField]
    public CFSpellDefense Defense = CFSpellDefense.None;

    /// <summary>
    /// Remaster basic save: critical success none, success half, failure full, critical failure double.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BasicSave = true;

    [DataField, AutoNetworkedField]
    public int DiceCount;

    [DataField, AutoNetworkedField]
    public int DiceSize;

    [DataField, AutoNetworkedField]
    public bool AddSpellModifier;

    [DataField, AutoNetworkedField]
    public int FlatDamage;

    [DataField, AutoNetworkedField]
    public string DamageType = "force";

    [DataField, AutoNetworkedField]
    public int HealDice;

    [DataField, AutoNetworkedField]
    public int HealDie;

    [DataField, AutoNetworkedField]
    public int HealFlat;

    [DataField, AutoNetworkedField]
    public int AcBonus;

    [DataField, AutoNetworkedField]
    public int AttackBonus;

    /// <summary>Subtracted from the target's AC on a spell attack (phase bolt's circumstance cut).</summary>
    [DataField, AutoNetworkedField]
    public int TargetAcPenalty;

    /// <summary>Helpful spells affect you and creatures you cannot harm.</summary>
    [DataField, AutoNetworkedField]
    public bool Friendly;

    [DataField, AutoNetworkedField]
    public bool IncludeSelf;

    [DataField, AutoNetworkedField]
    public CFSpellRider Rider = CFSpellRider.None;

    [DataField, AutoNetworkedField]
    public int PushFeet = 5;

    [DataField, AutoNetworkedField]
    public float DurationSeconds = 60f;

    [DataField, AutoNetworkedField]
    public EntProtoId BoltPrototype = "CFSpellBolt";

    /// <summary>
    /// Highest number of bolts one cast can fire. 1 is a single shot.
    /// Force Barrage is 3: the caster picks 1, 2, or 3, and the spell slot is spent once.
    /// </summary>
    [DataField]
    public int MaxBolts = 1;

    /// <summary>Bolts chosen for the cast that is being aimed. The server copies this from the perform request.</summary>
    [DataField]
    public int ChosenBolts = 1;

    /// <summary>Opens Food, Lift, Make, and Tidy before the cast. Prestidigitation.</summary>
    [DataField]
    public bool TrickMenu;

    /// <summary>Trick chosen for this cast. The server copies this from the perform request.</summary>
    [DataField]
    public byte ChosenTrick;

    /// <summary>Bolts of this volley that still need a click. Counted down on the client as you aim.</summary>
    public int VolleyLeft;

    /// <summary>The first bolt of this volley has been sent. Later clicks do not pay the action cost or the spell slot again.</summary>
    public bool VolleyArmed;

    /// <summary>This perform is the opening shot, so it pays for the whole volley.</summary>
    public bool PayThisShot;

    /// <summary>
    /// Puts the volley on a perform request. The first shot carries the full count.
    /// Each later shot is marked as a continuation and fires one stored bolt.
    /// </summary>
    public void WriteVolley(RequestPerformActionEvent request)
    {
        request.SpellTrick = ChosenTrick;
        if (MaxBolts <= 1)
        {
            request.SpellBolts = SelectedBolts();
            request.SpellVolleyContinue = false;
            return;
        }

        request.SpellBolts = (byte) Math.Clamp(ChosenBolts < 1 ? 1 : ChosenBolts, 1, MaxBolts);
        request.SpellVolleyContinue = VolleyArmed;
        VolleyArmed = true;
        if (VolleyLeft > 0)
            VolleyLeft--;
    }

    /// <summary>0 when this cast is not a multi-bolt spell. Otherwise the chosen count, clamped to <see cref="MaxBolts"/>.</summary>
    public byte SelectedBolts()
    {
        if (MaxBolts <= 1)
            return 0;

        var count = ChosenBolts < 1 ? 1 : ChosenBolts;
        return (byte) Math.Clamp(count, 1, MaxBolts);
    }
}

public enum CFSpellDelivery : byte
{
    Instant,
    Target,
    Projectile,
    Area,
}

public enum CFSpellAreaKind : byte
{
    None,
    Burst,
    Cone,
    Emanation,
    Line,
    Square,
}

public enum CFSpellDefense : byte
{
    None,
    SpellAttack,
    Reflex,
    Fortitude,
    Will,
}

public enum CFSpellRider : byte
{
    None,
    Stabilize,
    Light,
    TrueStrike,
    Sleep,
    Prone,
    Push,
    Paralyze,
}

public sealed partial class CFSpellCastInstantEvent : InstantActionEvent
{
}

public sealed partial class CFSpellCastTargetEvent : EntityTargetActionEvent
{
}

public sealed partial class CFSpellCastAreaEvent : WorldTargetActionEvent
{
}

public enum CFPrestidigitationTrick : byte
{
    None = 0,
    Food = 1,
    Lift = 2,
    Make = 3,
    Tidy = 4,
}
