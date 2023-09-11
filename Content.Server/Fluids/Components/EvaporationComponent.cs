using Content.Server.Fluids.EntitySystems;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Fluids.Components;

/// <summary>
/// Added to puddles that contain water so it may evaporate over time.
/// </summary>
[RegisterComponent, Access(typeof(PuddleSystem))]
public sealed partial class EvaporationComponent : Component
{
    /// <summary>
    /// The next time we remove the EvaporationSystem reagent amount from this entity.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("nextTick", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan NextTick = TimeSpan.Zero;

    /// <summary>
    /// How much evaporation occurs every tick.
    /// </summary>
    [DataField("evaporationAmount")]
    public FixedPoint2 EvaporationAmount = FixedPoint2.New(0.3);
}
