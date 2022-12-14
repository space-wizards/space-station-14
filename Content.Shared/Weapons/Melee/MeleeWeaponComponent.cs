using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Melee;

/// <summary>
/// When given to a mob lets them do unarmed attacks, or when given to an item lets someone wield it to do attacks.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed class MeleeWeaponComponent : Component
{
    /// <summary>
    /// Should the melee weapon's damage stats be examinable.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("hidden")]
    public bool HideFromExamine { get; set; } = false;

    /// <summary>
    /// Next time this component is allowed to light attack. Heavy attacks are wound up and never have a cooldown.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextAttack")]
    public TimeSpan NextAttack;

    /*
     * Melee combat works based around 2 types of attacks:
     * 1. Click attacks with left-click. This attacks whatever is under your mnouse
     * 2. Wide attacks with right-click + left-click. This attacks whatever is in the direction of your mouse.
     */

    /// <summary>
    /// How many times we can attack per second.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("attackRate")]
    public float AttackRate = 1f;

    /// <summary>
    /// Are we currently holding down the mouse for an attack.
    /// Used so we can't just hold the mouse button and attack constantly.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool Attacking = false;

    /// <summary>
    /// When did we start a heavy attack.
    /// </summary>
    /// <returns></returns>
    [ViewVariables(VVAccess.ReadWrite), DataField("windUpStart")]
    public TimeSpan? WindUpStart;

    /// <summary>
    /// How long it takes a heavy attack to windup.
    /// </summary>
    [ViewVariables]
    public TimeSpan WindupTime => AttackRate > 0 ? TimeSpan.FromSeconds(1 / AttackRate * HeavyWindupModifier) : TimeSpan.Zero;

    /// <summary>
    /// Heavy attack windup time gets multiplied by this value and the light attack cooldown.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("heavyWindupModifier")]
    public float HeavyWindupModifier = 1.5f;

    /// <summary>
    /// Light attacks get multiplied by this over the base <see cref="Damage"/> value.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("heavyDamageModifier")]
    public FixedPoint2 HeavyDamageModifier = FixedPoint2.New(2);

    /// <summary>
    /// Base damage for this weapon. Can be modified via heavy damage or other means.
    /// </summary>
    [DataField("damage", required:true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    [DataField("bluntStaminaDamageFactor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 BluntStaminaDamageFactor { get; set; } = 0.5f;

    // TODO: Temporarily 1.5 until interactionoutline is adjusted to use melee, then probably drop to 1.2
    /// <summary>
    /// Nearest edge range to hit an entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = 1.5f;

    /// <summary>
    /// Total width of the angle for wide attacks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("angle")]
    public Angle Angle = Angle.FromDegrees(60);

    [ViewVariables(VVAccess.ReadWrite), DataField("animation", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ClickAnimation = "WeaponArcPunch";

    [ViewVariables(VVAccess.ReadWrite), DataField("wideAnimation", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string WideAnimation = "WeaponArcSlash";

    // Sounds

    /// <summary>
    /// This gets played whenever a melee attack is done. This is predicted by the client.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundSwing")]
    public SoundSpecifier SwingSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg")
    {
        Params = AudioParams.Default.WithVolume(-3f).WithVariation(0.025f),
    };

    // We do not predict the below sounds in case the client thinks but the server disagrees. If this were the case
    // then a player may doubt if the target actually took damage or not.
    // If overwatch and apex do this then we probably should too.

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundHit")]
    public SoundSpecifier? HitSound;

    /// <summary>
    /// Plays if no damage is done to the target entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundNoDamage")]
    public SoundSpecifier NoDamageSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/tap.ogg");
}

/// <summary>
/// Event raised on entity in GetWeapon function to allow systems to manually
/// specify what the weapon should be.
/// </summary>
public sealed class GetMeleeWeaponEvent : HandledEntityEventArgs
{
    public EntityUid? Weapon;
}

[Serializable, NetSerializable]
public sealed class MeleeWeaponComponentState : ComponentState
{
    // None of the other data matters for client as they're not predicted.

    public float AttackRate;
    public bool Attacking;
    public TimeSpan NextAttack;
    public TimeSpan? WindUpStart;

    public string ClickAnimation;
    public string WideAnimation;
    public float Range;

    public MeleeWeaponComponentState(float attackRate, bool attacking, TimeSpan nextAttack, TimeSpan? windupStart, string clickAnimation, string wideAnimation, float range)
    {
        AttackRate = attackRate;
        Attacking = attacking;
        NextAttack = nextAttack;
        WindUpStart = windupStart;
        ClickAnimation = clickAnimation;
        WideAnimation = wideAnimation;
        Range = range;
    }
}
