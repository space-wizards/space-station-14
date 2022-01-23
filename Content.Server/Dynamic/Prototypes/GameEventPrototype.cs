using System;
using System.Collections.Generic;
using Content.Server.Dynamic.Abstract;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;

namespace Content.Server.Dynamic.Prototypes;

/// <summary>
///     Corresponds to a single game event that dynamic can pick using its threat pool.
/// </summary>
[Prototype("gameEvent")]
public class GameEventPrototype : IPrototype
{
    [DataField("id", required: true)]
    public string ID { get; } = default!;

    /// <summary>
    ///     A player-friendly name for this event.
    /// </summary>
    [DataField("name", required: true)]
    public string Name = default!;

    /// <summary>
    ///     A player-friendly description of this event.
    /// </summary>
    [DataField("description")]
    public string Description = String.Empty;

    /// <summary>
    ///     A list of tags this event has.
    /// </summary>
    [DataField("eventTags", customTypeSerializer:typeof(PrototypeIdHashSetSerializer<GameEventTagPrototype>))]
    public HashSet<string> EventTags = default!;

    /// <summary>
    ///     A bitfield of event types.
    /// </summary>
    [DataField("eventTypes", customTypeSerializer:typeof(FlagSerializer<DynamicEventType>))]
    public int EventTypes = (int) DynamicEventTypeEnum.Roundstart;

    /// <summary>
    ///     The threat cost of this event. Higher is more dangerous.
    /// </summary>
    [DataField("threat")]
    public float ThreatCost = 250f;

    /// <summary>
    ///     How hard is this event weighted against other events? An integer from 0-9.
    /// </summary>
    [DataField("weight")]
    public int Weight = 5;

    /// <summary>
    ///     The maximum amount of time after which an event will no longer be checked for refunds. In seconds.
    /// </summary>
    [DataField("maxRefundTime")]
    public float MaxRefundTime = 10.0f * 60f;

    /// <summary>
    ///     A list of candidate conditions for this event.
    /// </summary>
    [DataField("candidateConditions")]
    public List<CandidateCondition> CandidateConditions = default!;

    /// <summary>
    ///     A list of event conditions, checked before it is purchased to determine if it can be run.
    /// </summary>
    [DataField("eventConditions")]
    public List<GameEventCondition> EventConditions = default!;

    /// <summary>
    ///     A list of event conditions, checked every refund internal in Dynamic before <see cref="MaxRefundTime"/> to
    ///     determine if it should be refunded.
    ///
    ///     No refund conditions means this event will never be refunded.
    /// </summary>
    [DataField("refundConditions")]
    public List<GameEventCondition> RefundConditions = default!;

    /// <summary>
    ///     A list of effects to be run when this event is purchased.
    /// </summary>
    [DataField("effects")]
    public List<GameEventEffect> EventEffects = default!;
}
