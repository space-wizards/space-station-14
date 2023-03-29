using Content.Server.Medical.Bloodstream.Systems;
using Content.Shared.FixedPoint;

namespace Content.Server.Medical.Bloodstream.Components;

[RegisterComponent, Access(typeof(BloodstreamSystem))]
public sealed class BleedInflicterComponent : Component
{
    //How much bloodloss to apply
    [DataField("bloodLoss", required: true)]
    public FixedPoint2 BloodLoss;
}
