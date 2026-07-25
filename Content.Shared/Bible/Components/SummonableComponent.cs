using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Bible.Components;

/// <summary>
/// This lets you summon a mob or item with an alternative verb on the item
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(BibleSystem))]
public sealed partial class SummonableComponent : Component
{
    /// <summary>
    /// Default sound to play when entity is summoned.
    /// </summary>
    private static readonly ProtoId<SoundCollectionPrototype> DefaultSummonSound = new("Summon");

    /// <summary>
    /// Sound to play when entity is summoned.
    /// </summary>
    [DataField]
    public SoundSpecifier SummonSound = new SoundCollectionSpecifier(DefaultSummonSound, AudioParams.Default.WithVolume(-4f));

    /// <summary>
    /// Used for a special item only the Chaplain can summon. Usually a mob, but supports regular items too.
    /// </summary>
    [DataField("summonEntity")]
    public EntProtoId? SummonEntityPrototype;

    /// <summary>
    /// The summon action for component to use.
    /// </summary>
    [DataField("summonAction")]
    public EntProtoId SummonActionPrototype = "ActionBibleSummon";

    /// <summary>
    /// The specific creature this summoned, if the SpecialItemPrototype has a mobstate.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? SummonedEntity = null;

    /// <summary>
    /// The created summon action.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SummonActionEntity;

    /// <summary>
    /// Whether or not user must have <see cref="BibleUserComponent"/>.
    /// </summary>
    [DataField]
    public bool RequiresBibleUser = true;

    /// <summary>
    /// Whether or not summoning is allowed at the moment.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public bool CanSummon = true;

    /// <summary>
    /// Loc string for the summon alt-verb text.
    /// </summary>
    [DataField]
    public string SummonVerbText = "bible-summon-verb";

    /// <summary>
    /// Loc string shown when a summon has been requested and is awaiting a soul.
    /// </summary>
    [DataField]
    public string SummonRequestedText = "bible-summon-requested";

    /// <summary>
    /// Loc string shown when the familiar has respawned and is ready to summon again.
    /// </summary>
    [DataField]
    public string SummonRespawnReadyText = "bible-summon-respawn-ready";

    /// <summary>
    /// Cooldown before being able to summon a new entity.
    /// </summary>
    [DataField]
    public TimeSpan RespawnCooldown = TimeSpan.FromSeconds(180);

    /// <summary>
    /// Time after which summoning will be allowed.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField, AutoNetworkedField]
    public TimeSpan RespawnTime = TimeSpan.Zero;
}
