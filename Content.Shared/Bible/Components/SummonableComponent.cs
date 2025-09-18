using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Bible.Components;

/// <summary>
/// This lets you summon a mob or item with an alternative verb on the item.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[AutoGenerateComponentPause]
public sealed partial class SummonableComponent : Component
{
    /// <summary>
    /// Sound to play when entity is summoned.
    /// </summary>
    [DataField]
    public SoundSpecifier SummonSound = new SoundCollectionSpecifier("Summon", AudioParams.Default.WithVolume(-4f));

    /// <summary>
    /// Used for a special item only the Chaplain can summon. Usually a mob, but supports regular items too.
    /// </summary>
    [DataField("specialItem")]
    public EntProtoId? SpecialItemPrototype = null;

    /// <summary>
    /// Is the summoned entity is alive?
    /// </summary>
    public bool AlreadySummoned = false;

    /// <summary>
    /// If the entity's user should have <see cref="BibleUserComponent"/>.
    /// </summary>
    [DataField]
    public bool RequiresBibleUser = true;

    /// <summary>
    /// The specific creature this summoned, if the SpecialItemPrototype has a mobstate.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Summon = null;

    [DataField]
    public EntProtoId SummonAction = "ActionBibleSummon";

    [DataField, AutoNetworkedField]
    public EntityUid? SummonActionEntity;

    /// <summary>
    /// Used for respawning.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? RespawnEndTime;

    /// <summary>
    /// Cooldown between entity summon attempts.
    /// </summary>
    [DataField]
    public TimeSpan RespawnInterval = TimeSpan.FromSeconds(180);
}
