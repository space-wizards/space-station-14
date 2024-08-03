using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;

namespace Content.Server.Chemistry.Components;

[RegisterComponent]
public sealed partial class SolutionVacuumCleanerComponent : Component
{
    [DataField("fixedTransferAmount")]
    public FixedPoint2 FixedTransferAmount = FixedPoint2.New(25);

    [DataField("delay")]
    public TimeSpan Delay = TimeSpan.FromMilliseconds(500);

    [DataField("doAfterId")]
    public DoAfterId? DoAfterId = null;
}
