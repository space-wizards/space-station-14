using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Fluids.Components;

/// <summary>
/// Added to puddles that contain water so it may evaporate over time.
/// </summary>
[NetworkedComponent, AutoGenerateComponentPause, AutoGenerateComponentState]
[RegisterComponent, Access(typeof(SharedPuddleSystem))]
public sealed partial class EvaporationComponent : Component
{
    /// <summary>
    /// The next time we remove the EvaporationSystem reagent amount from this entity.
    /// </summary>
    [AutoNetworkedField, AutoPausedField]
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick;

    /// <summary>
    /// Evaporation factor. Multiplied by the evaporating speed of the reagent.
    /// </summary>
    [DataField]
    public FixedPoint2 EvaporationAmount = FixedPoint2.New(1);

    /// <summary>
    /// The effect spawned when the puddle fully evaporates.
    /// </summary>
    [DataField]
    public EntProtoId EvaporationEffect = "PuddleSparkle";
}
