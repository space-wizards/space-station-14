using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChargerComponent : Component
{
    /// <summary>
    /// The charge rate of the charger, in watts.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ChargeRate = 20.0f;

    /// <summary>
    /// Passive draw when no power cell is inserted, in watts.
    /// This should be larger than 0 or the charger will be considered as powered even without a LV supply.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float PassiveDraw = 1f;

    /// <summary>
    /// The container ID that is holds the entities being charged.
    /// </summary>
    [DataField(required: true)]
    public string SlotId = string.Empty;

    /// <summary>
    /// A whitelist for what entities can be charged by this Charger.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityWhitelist? Whitelist;

    /// <summary>
    /// Indicates whether the charger is portable and thus subject to EMP effects
    /// and bypasses checks for transform, anchored, and ApcPowerReceiverComponent.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Portable = false;
}

[Serializable, NetSerializable]
public enum CellChargerStatus
{
    Off,
    Empty,
    Charging,
    Charged,
}

[Serializable, NetSerializable]
public enum CellVisual
{
    Occupied, // If there's an item in it
    Light,
}
