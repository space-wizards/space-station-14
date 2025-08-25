using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ChargerComponent : Component
{
    [ViewVariables]
    public CellChargerStatus Status;

    /// <summary>
    /// The charge rate of the charger, in watts
    /// </summary>
    [DataField]
    public float ChargeRate = 20.0f;

    /// <summary>
    /// The container ID that is holds the entities being charged.
    /// </summary>
    [DataField(required: true)]
    public string SlotId = string.Empty;

    /// <summary>
    /// A whitelist for what entities can be charged by this Charger.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Indicates whether the charger is portable and thus subject to EMP effects
    /// and bypasses checks for transform, anchored, and ApcPowerReceiverComponent.
    /// </summary>
    [DataField]
    public bool Portable = false;
}
