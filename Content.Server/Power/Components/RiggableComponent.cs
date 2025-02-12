using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;

namespace Content.Server.Power.Components;

[RegisterComponent]
public sealed partial class RiggableComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool IsRigged;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public string Solution = "battery";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("reagent")]
    public ReagentQuantity RequiredQuantity = new("Plasma", FixedPoint2.New(5), null);
}
