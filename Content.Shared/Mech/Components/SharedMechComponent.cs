using Content.Shared.Actions;
using Content.Shared.Actions.ActionTypes;
using Robust.Shared.Containers;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Mech.Components;

//TODO: make some fucking network replication
public abstract class SharedMechComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public ContainerSlot RiderSlot = default!;
    [ViewVariables]
    public readonly string RiderSlotId = "mech-rider-slot";

    [ViewVariables(VVAccess.ReadWrite)]
    public bool Broken = false;

    [ViewVariables]
    public EntityUid? CurrentSelectedEquipment;
    [ViewVariables(VVAccess.ReadWrite)]
    public Container EquipmentContainer = default!;
    [ViewVariables]
    public readonly string EquipmentContainerId = "mech-equipment-container";

    [DataField("mechToggleAction", customTypeSerializer: typeof(PrototypeIdSerializer<InstantActionPrototype>))]
    public string MechToggleAction = "MechToggleEquipment";

    #region Visualizer States
    [DataField("baseState")]
    public string? BaseState;
    [DataField("openState")]
    public string? OpenState;
    [DataField("brokenState")]
    public string? BrokenState;
    #endregion
}

[Serializable, NetSerializable]
public enum MechVisuals : byte
{
    Open, //whether or not it's open and has a rider
    Broken //if it broke and no longer works.
}

[Serializable, NetSerializable]
public enum MechVisualLayers : byte
{
    Base
}

public sealed class MechToggleEquipmentEvent : InstantActionEvent
{

}
