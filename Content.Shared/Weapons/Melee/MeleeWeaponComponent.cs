using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Weapons.Melee;

[RegisterComponent, NetworkedComponent]
public sealed class MeleeWeaponComponent : Component
{
    // TODO: When predicted comp change.
    [ViewVariables]
    public bool Active;

    // TODO: Can't use accumulator because we'd need an active component and client can't predict changing it.
    /// <summary>
    /// How much windup time have we accumulated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("windupAccumulator")]
    public float WindupAccumulator = 0f;

    /// <summary>
    /// How long it takes an attack to windup.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("windupTime")]
    public float WindupTime = 1f;

    [DataField("damage", required:true)]
    [ViewVariables(VVAccess.ReadWrite)]
    public DamageSpecifier Damage = default!;

    [DataField("bluntStaminaDamageFactor")]
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 BluntStaminaDamageFactor { get; set; } = 0.5f;

    [ViewVariables(VVAccess.ReadWrite), DataField("range")]
    public float Range = SharedInteractionSystem.InteractionRange;

    /// <summary>
    /// Total width of the angle for wide attacks.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("angle")]
    public Angle Angle = Angle.Zero;

    [ViewVariables(VVAccess.ReadWrite), DataField("animation", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string Animation = "WeaponArcThrust";

    // Sounds

    /// <summary>
    /// This gets played whenever a melee attack is done. This is predicted by the client.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundSwing")]
    public SoundSpecifier SwingSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/punchmiss.ogg");

    // We do not predict the below sounds in case the client thinks but the server disagrees. If this were the case
    // then a player may doubt if the target actually took damage or not.
    // If overwatch and apex do this then we probably should too.

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("hitSound")]
    public SoundSpecifier? HitSound;

    /// <summary>
    /// This gets played if damage is done.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundDamage")]
    public SoundSpecifier? DamageSound;

    /// <summary>
    /// Plays if no damage is done to the target entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("soundNoDamage")]
    public SoundSpecifier NoDamageSound { get; set; } = new SoundPathSpecifier("/Audio/Weapons/tap.ogg");
}
