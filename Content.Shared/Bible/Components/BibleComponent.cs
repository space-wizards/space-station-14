using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Bible.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class BibleComponent : Component
{
    /// <summary>
    /// Default sound when bible hits somebody.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultBibleHit = new("BibleHit");

    /// <summary>
    /// Sound to play when bible hits somebody.
    /// </summary>
    [DataField]
    public SoundSpecifier BibleHitSound = new SoundCollectionSpecifier(DefaultBibleHit, AudioParams.Default.WithVolume(-4f));

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
    /// Chance the bible will fail to heal someone with no helmet.
    /// </summary>
    [DataField]
    public float FailChance = 0.34f;

    [DataField("sizzleSound")]
    public SoundSpecifier SizzleSoundPath = new SoundPathSpecifier("/Audio/Effects/lightburn.ogg");
    [DataField("healSound")]
    public SoundSpecifier HealSoundPath = new  SoundPathSpecifier("/Audio/Effects/holy.ogg");

    [DataField]
    public string LocPrefix = "bible";
}
