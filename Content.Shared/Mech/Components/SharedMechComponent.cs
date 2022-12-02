using Content.Shared.Actions.ActionTypes;
using Content.Shared.FixedPoint;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mech.Components;

public abstract class SharedMechComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Integrity;

    [DataField("maxIntegrity")]
    public FixedPoint2 MaxIntegrity = 300;

    [ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 Energy = 0;

    [DataField("maxEnergy")]
    public FixedPoint2 MaxEnergy = 0;

    [ViewVariables]
    public ContainerSlot BatterySlot = default!;

    [ViewVariables]
    public readonly string BatterySlotId = "mech-battery-slot";

    /// <summary>
    /// A multiplier used to calculate how much of the damage done to a mech
    /// is transfered to the pilot
    /// </summary>
    [DataField("mechToPilotDamageMultiplier")]
    public float MechToPilotDamageMultiplier;

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Broken = false;

    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot PilotSlot = default!;

    [ViewVariables]
    public readonly string PilotSlotId = "mech-pilot-slot";

    [ViewVariables]
    public EntityUid? CurrentSelectedEquipment;

    [DataField("maxEquipmentAmount")]
    public int MaxEquipmentAmount = 3;

    [DataField("equipmentWhitelist")]
    public EntityWhitelist? EquipmentWhitelist;

    [ViewVariables(VVAccess.ReadWrite)]
    public Container EquipmentContainer = default!;

    [ViewVariables]
    public readonly string EquipmentContainerId = "mech-equipment-container";

    #region Action Prototypes
    [DataField("mechCycleAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string MechCycleAction = "MechCycleEquipment";
    [DataField("mechUiAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string MechUiAction = "MechOpenUI";
    [DataField("mechEjectAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string MechEjectAction = "MechEject";
    #endregion

    #region Visualizer States
    [DataField("baseState")]
    public string? BaseState;
    [DataField("openState")]
    public string? OpenState;
    [DataField("brokenState")]
    public string? BrokenState;
    #endregion
}

/// <summary>
/// Contains network state for SharedMechComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class MechComponentState : ComponentState
{
    public FixedPoint2 Integrity;
    public FixedPoint2 MaxIntegrity;
    public FixedPoint2 Energy;
    public FixedPoint2 MaxEnergy;
    public EntityUid? CurrentSelectedEquipment;
    public bool Broken;
}
