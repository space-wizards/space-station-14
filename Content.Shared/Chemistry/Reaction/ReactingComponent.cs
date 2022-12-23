using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Chemistry.Reaction;

[RegisterComponent]
public sealed class ReactingComponent : Component
{
    public readonly Dictionary<Solution, Dictionary<ReactionSpecification, ReactionData>> ReactionGroups = new();
    public bool QueuedForDeletion { get; internal set; } = false;


    #region Update Timing

    [DataField("updatePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedChemicalReactionSystem))]
    public TimeSpan TargetUpdatePeriod { get; internal set; } = TimeSpan.FromSeconds(0.2);

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedChemicalReactionSystem))]
    public TimeSpan NextUpdateTime { get; internal set; } = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [Access(typeof(SharedChemicalReactionSystem))]
    public TimeSpan LastUpdateTime { get; internal set; } = default!;

    #endregion UpdateTiming

}

[DataDefinition]
public sealed class ReactionData
{
    public ReactionState State = ReactionState.None;

    /// <summary>
    /// The time at which this reaction began.
    /// </summary>
    [DataField("startTime")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SharedChemicalReactionSystem))]
    public readonly TimeSpan StartTime;

    /// <summary>
    /// The last time this reaction was updated.
    /// </summary>
    [DataField("lastTime")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SharedChemicalReactionSystem))]
    public TimeSpan LastTime;

    /// <summary>
    /// How long this reaction has been ongoing so far.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan TotalTime => LastTime - StartTime;

    /// <summary>
    /// The total quantity of reactants that have reacted so far.
    /// </summary>
    [DataField("totalQuantity")]
    [ViewVariables(VVAccess.ReadOnly)]
    [Access(typeof(SharedChemicalReactionSystem))]
    public FixedPoint2 TotalQuantity = FixedPoint2.Zero;

    public ReactionData(TimeSpan startTime)
    {
        StartTime = startTime;
        LastTime = startTime;
    }
}

public enum ReactionState : sbyte
{
    Cancelled = -1,
    None = 0,
    Starting = 1,
    Running = 2,
    Stopping = 3,
    Stopped = 4,
}
