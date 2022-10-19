using Robust.Shared.Serialization;

namespace Content.Shared.Pointing;

public abstract class SharedPointingSystem : EntitySystem
{
    [Serializable, NetSerializable]
    protected sealed class PointingArrowComponentState : ComponentState
    {
        public TimeSpan EndTime;

        public PointingArrowComponentState(TimeSpan endTime)
        {
            EndTime = endTime;
        }
    }
}
