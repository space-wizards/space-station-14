using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

/// <summary>
/// Attempts to link with a nearby <see cref="ApcPowerProviderComponent"/>s
/// so that it can receive power from a <see cref="IApcNet"/>.
/// </summary>
[NetworkedComponent]
public abstract partial class SharedApcPowerReceiverComponent : Component
{
    /// <summary>
    /// If true, this entity either doesn't need power, or is currently receiving the power it needs.
    /// </summary>
    [ViewVariables]
    public bool Powered;

    /// <summary>
    /// When false, causes this to appear powered even if not receiving power from an Apc.
    /// </summary>
    [DataField]
    public bool NeedsPower = true;

    /// <summary>
    /// When true, causes this to never appear powered.
    /// </summary>
    [DataField]
    public virtual bool PowerDisabled { get; set; }

    /// <summary>
    /// Amount of power this needs from an APC in watts to function.
    /// </summary>
    [DataField("powerLoad")]
    public virtual float Load { get; set; }
}
