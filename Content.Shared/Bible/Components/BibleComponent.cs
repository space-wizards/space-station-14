using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Bible.Components;

/// <summary>
/// Marks an entity as Bible that heals somebody on interaction.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(BibleSystem))]
public sealed partial class BibleComponent : Component
{
    /// <summary>
    /// Default sound when Bible hits somebody.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultBibleHitSound = new("BibleHit");

    /// <summary>
    /// Default sound when Bible fails to heal somebody.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultBibleSizzleSound = new("BibleSizzle");

    /// <summary>
    /// Default sound when Bible heals somebody.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultBibleHealSound = new("BibleHeal");

    /// <summary>
    /// Sound to play when Bible hits somebody.
    /// </summary>
    [DataField]
    public SoundSpecifier BibleHitSound = new SoundCollectionSpecifier(DefaultBibleHitSound, AudioParams.Default.WithVolume(-4f));

    /// <summary>
    /// Sound to play when Bible fails to heal somebody.
    /// </summary>
    [DataField]
    public SoundSpecifier SizzleSound = new SoundCollectionSpecifier(DefaultBibleSizzleSound);

    /// <summary>
    /// Sound to play when Bible heals somebody.
    /// </summary>
    [DataField]
    public SoundSpecifier HealSound = new SoundCollectionSpecifier(DefaultBibleHealSound);

    /// <summary>
    /// Damage that will be healed on a success.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = default!;

    /// <summary>
    /// Damage that will be dealt on a failure.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier DamageOnFail = default!;

    /// <summary>
    /// Damage that will be dealt when a non-chaplain attempts to heal.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier DamageOnUntrainedUse = default!;

    /// <summary>
    /// Chance theBbible will fail to heal someone with no helmet.
    /// </summary>
    [DataField]
    public float FailChance = 0.34f;

    /// <summary>
    /// Loc string played when a non-chaplain attempts to use the bible.
    /// </summary>
    [DataField]
    public string SizzleText = "bible-sizzle";

    /// <summary>
    /// Loc string shown to others when the bible fails to heal the target.
    /// </summary>
    [DataField]
    public string HealFailOthersText = "bible-heal-fail-others";

    /// <summary>
    /// Loc string shown to the user when the bible fails to heal the target.
    /// </summary>
    [DataField]
    public string HealFailSelfText = "bible-heal-fail-self";

    /// <summary>
    /// Loc string shown to others when the bible successfully heals the target.
    /// </summary>
    [DataField]
    public string HealSuccessOthersText = "bible-heal-success-others";

    /// <summary>
    /// Loc string shown to the user when the bible successfully heals the target.
    /// </summary>
    [DataField]
    public string HealSuccessSelfText = "bible-heal-success-self";

    /// <summary>
    /// Loc string shown to others when the bible hits a target without wounds.
    /// </summary>
    [DataField]
    public string HealSuccessNoneOthersText = "bible-heal-success-none-others";

    /// <summary>
    /// Loc string shown to the user when the bible hits a target without wounds.
    /// </summary>
    [DataField]
    public string HealSuccessNoneSelfText = "bible-heal-success-none-self";

    /// <summary>
    /// A short light effect to display when successfully healing someone.
    /// </summary>
    [DataField]
    public EntProtoId? HealingLightEffect = "HolyLightEffect";
}
