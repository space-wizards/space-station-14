using Content.Shared.Singularity.Components;

namespace Content.Server.Singularity.Components;

/// <summary>
///
/// </summary>
[RegisterComponent]
[ComponentReference(typeof(SharedEventHorizonComponent))]
public sealed class EventHorizonComponent : SharedEventHorizonComponent
{
    /// <summary>
    ///     Whether the entity this event horizon is attached to is being consumed by another event horizon.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public bool BeingConsumedByAnotherEventHorizon = false;

    /// <summary>
    ///
    /// </summary>
    [DataField("consumePeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ConsumePeriod = 0.5f;

    /// <summary>
    ///
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public float _timeSinceLastConsumeWave = float.PositiveInfinity;
}
