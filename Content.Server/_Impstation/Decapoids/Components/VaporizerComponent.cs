using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.Decapoids.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class VaporizerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public string LiquidTank = "waterTank";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<ReagentPrototype> ExpectedReagent = "Water";

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Gas OutputGas = Gas.WaterVapor;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 MaxPressure = Atmospherics.OneAtmosphere * 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ReagentToMoles = 0.07f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 ReagentPerSecond = 0.09;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ProcessDelay = TimeSpan.FromMilliseconds(200);

    [DataField(readOnly: true), ViewVariables(VVAccess.ReadOnly)]
    [AutoPausedField]
    public TimeSpan NextProcess = new();
}
