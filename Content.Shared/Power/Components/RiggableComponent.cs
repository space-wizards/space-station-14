using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RiggableSystem))]
public sealed class RiggableComponent : Component
{
    public const string SolutionName = "battery";

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isRigged")]
    public bool IsRigged;
}
