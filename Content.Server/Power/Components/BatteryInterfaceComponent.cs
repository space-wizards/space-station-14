using Content.Server.Power.EntitySystems;
using Content.Shared.Power;

namespace Content.Server.Power.Components;

/// <summary>
/// Necessary component for battery management UI for SMES/substations.
/// </summary>
/// <seealso cref="BatteryUiKey.Key"/>
/// <seealso cref="BatteryInterfaceSystem"/>
[RegisterComponent]
public sealed partial class BatteryInterfaceComponent : Component
{
    /// <summary>
    /// The maximum charge rate users can configure through the UI.
    /// </summary>
    [DataField]
    public float MaxChargeRate;

    /// <summary>
    /// The minimum charge rate users can configure through the UI.
    /// </summary>
    [DataField]
    public float MinChargeRate;

    /// <summary>
    /// The maximum discharge rate users can configure through the UI.
    /// </summary>
    [DataField]
    public float MaxSupply;

    /// <summary>
    /// The minimum discharge rate users can configure through the UI.
    /// </summary>
    [DataField]
    public float MinSupply;
}
