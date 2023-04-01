using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Medical.Surgery;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSurgeryRealmSystem))]
public sealed class SurgeryRealmSlidingComponent : Component
{
    public float FinalY;
    public MapCoordinates SectionPos;
    public bool Fired;
}

[Serializable, NetSerializable]
public sealed class SurgeryRealmSlidingComponentState : ComponentState
{
    public float FinalY { get; }
    public MapCoordinates SectionPos { get; }

    public SurgeryRealmSlidingComponentState(float finalY, MapCoordinates sectionPos)
    {
        FinalY = finalY;
        SectionPos = sectionPos;
    }
}
