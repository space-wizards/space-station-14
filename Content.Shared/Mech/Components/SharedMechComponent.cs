using Content.Shared.Actions.ActionTypes;
using Content.Shared.Whitelist;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mech.Components;

public abstract class SharedMechComponent : Component
{
    //TODO: implement integrity
    [ViewVariables(VVAccess.ReadWrite)]
    public float Integrity;

    [DataField("maxIntegrity")]
    public float MaxIntegrity = 300;

    [ViewVariables(VVAccess.ReadWrite)]
    public float Energy;

    [DataField("maxEnergy")]
    public float MaxEnergy = 200;

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
    public float Integrity;
    public float MaxIntegrity;
    public float Energy;
    public float MaxEnergy;
    public EntityUid? CurrentSelectedEquipment;
    public bool Broken;
}
