// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Goobstation.Factory.Slots;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Goobstation.Factory;

[RegisterComponent, NetworkedComponent, Access(typeof(RoboticArmSystem))]
[AutoGenerateComponentState(true, fieldDeltas: true), AutoGenerateComponentPause]
public sealed partial class RoboticArmComponent : Component
{
    #region Linking
    /// <summary>
    /// Machine linked to the input port.
    /// Might not always exist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? InputMachine;

    /// <summary>
    /// Sink port on this arm that machines link to.
    /// </summary>
    [DataField]
    public ProtoId<SinkPortPrototype> InputPort = "RoboticArmInput";

    /// <summary>
    /// The source port of the linked input machine.
    /// This controls which item slot etc gets pulled from.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SourcePortPrototype>? InputMachinePort;

    /// <summary>
    /// The resolved automation output slot of the input machine to take items from.
    /// </summary>
    [ViewVariables]
    public AutomationSlot? InputSlot;

    /// <summary>
    /// Machine linked to the output port.
    /// Might not always exist.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? OutputMachine;

    /// <summary>
    /// Source port on this arm that machines link from.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> OutputPort = "RoboticArmOutput";

    /// <summary>
    /// The sink port of the linked output machine.
    /// This controls which item slot etc gets inserted into.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<SinkPortPrototype>? OutputMachinePort;

    /// <summary>
    /// The resolved automation input slot of the output machine to insert items into.
    /// </summary>
    [ViewVariables]
    public AutomationSlot? OutputSlot;

    /// <summary>
    /// Signal port invoked after an item gets moved.
    /// </summary>
    [DataField]
    public ProtoId<SourcePortPrototype> MovedPort = "RoboticArmMoved";
    #endregion

    #region Item Slot
    /// <summary>
    /// Item slot that stores the held item.
    /// </summary>
    [DataField]
    public string ItemSlotId = "robotic_arm_item";

    /// <summary>
    /// The item slot cached on init.
    /// </summary>
    [ViewVariables]
    public ItemSlot ItemSlot = default!;

    /// <summary>
    /// The currently held item.
    /// </summary>
    [ViewVariables]
    public EntityUid? HeldItem => ItemSlot.Item;

    /// <summary>
    /// Whether an item is currently held.
    /// </summary>
    public bool HasItem => ItemSlot.HasItem;
    #endregion

    #region Input Items
    /// <summary>
    /// Fixture to look for input items with when no input machine is linked.
    /// </summary>
    [DataField]
    public string InputFixtureId = "robotic_arm_input";

    /// <summary>
    /// Items currently colliding with <see cref="InputFixtureId"/> and whether their CollisionWake was enabled.
    /// When items start to collide they get pushed to the end.
    /// When picking up items the last value is taken.
    /// This is essentially a FILO queue.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(NetEntity, bool)> InputItems = new();
    #endregion

    #region Arm Moving
    /// <summary>
    /// How long it takes to move an item.
    /// </summary>
    [DataField]
    public TimeSpan MoveDelay = TimeSpan.FromSeconds(0.6);

    /// <summary>
    /// When the arm will next move to the input or output.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoNetworkedField, AutoPausedField]
    public TimeSpan? NextMove;

    /// <summary>
    /// Sound played when moving an item.
    /// </summary>
    [DataField]
    public SoundSpecifier? MoveSound;
    #endregion

    #region Power

    /// <summary>
    /// Power used when idle.
    /// </summary>
    [DataField]
    public float IdlePowerDraw = 50f;

    /// <summary>
    /// Power used when moving items.
    /// </summary>
    [DataField]
    public float MovingPowerDraw = 1000f; // imp edit to avoid power flicker

    #endregion
}

[Serializable, NetSerializable]
public enum RoboticArmVisuals : byte
{
    HasItem
}

[Serializable, NetSerializable]
public enum RoboticArmLayers : byte
{
    Arm,
    Powered
}
