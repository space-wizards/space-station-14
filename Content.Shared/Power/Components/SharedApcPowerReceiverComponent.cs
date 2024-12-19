using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[NetworkedComponent]
public abstract partial class SharedApcPowerReceiverComponent : Component
{
    [ViewVariables]
    public bool Powered;

    [ViewVariables]
    public virtual bool NeedsPower { get; set; }

    [ViewVariables]
    public virtual bool PowerDisabled { get; set; }
}
