using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Silicons.Bots;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class RobotFenceComponent : Component
{
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public TimeSpan UpdatePeriod = TimeSpan.FromSeconds(1);

    [DataField]
    public FixedPoint2 BeamRange = 8f;

    [DataField(readOnly: true)]
    public int BeamSubSteps;

    [DataField(readOnly: true)]
    public List<EntityUid?> BeamEntities = [];
}
