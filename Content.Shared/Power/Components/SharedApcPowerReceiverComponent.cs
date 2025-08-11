using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[NetworkedComponent]
public abstract partial class SharedApcPowerReceiverComponent : Component
{
    [ViewVariables]
    public bool Powered;

    /// <summary>
    ///     When false, causes this to appear powered even if not receiving power from an Apc.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public virtual bool NeedsPower { get; set;}

    /// <summary>
    ///     When true, causes this to never appear powered.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public virtual bool PowerDisabled { get; set; }
}
