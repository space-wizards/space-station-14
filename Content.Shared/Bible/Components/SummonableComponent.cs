using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Bible.Components;

/// <summary>
/// This lets you summon a mob or item with an alternative verb on the item.
/// </summary>
[RegisterComponent, NetworkedComponent]
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
    /// Is the summoned entity is alive.
    /// </summary>
    public bool AlreadySummoned = false;

    [DataField]
    public bool RequiresBibleUser = true;

    /// <summary>
    /// The specific creature this summoned, if the SpecialItemPrototype has a mobstate.
    /// </summary>
    [ViewVariables]
    public EntityUid? Summon = null;

    [DataField]
    public EntProtoId SummonAction = "ActionBibleSummon";

    [DataField]
    public EntityUid? SummonActionEntity;

    /// <summary>
    /// Used for respawning.
    /// </summary>
    [DataField]
    public float Accumulator = 0f;

    /// <summary>
    /// Cooldown between entity summon attempts.
    /// </summary>
    [DataField]
    public float RespawnTime = 180f;
}
