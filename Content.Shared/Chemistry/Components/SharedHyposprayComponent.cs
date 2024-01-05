using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

[NetworkedComponent()]
public abstract partial class SharedHyposprayComponent : Component
{
    [DataField("solutionName")]
    public string SolutionName = "hypospray";
    // anti hypospray begin
    [DataField("canPenetrate")]
    public bool CanPenetrate = false;
    // anti hypospray end
}

[Serializable, NetSerializable]
public sealed class HyposprayComponentState : ComponentState
{
    public FixedPoint2 CurVolume { get; }
    public FixedPoint2 MaxVolume { get; }

    public HyposprayComponentState(FixedPoint2 curVolume, FixedPoint2 maxVolume)
    {
        CurVolume = curVolume;
        MaxVolume = maxVolume;
    }
}
