using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.CosmicCult.Components;

/// <summary>
/// Marker component for entities under the effect of Astral Shift.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CosmicShiftingComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))] [AutoPausedField]
    public TimeSpan? ShiftMoveTimer;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))] [AutoPausedField]
    public TimeSpan? ShiftEndTimer;

    public MapCoordinates DestinationCoordinates;
}
