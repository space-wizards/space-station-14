using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.FixedPoint;
using Content.Shared.Storage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// A machine that dispenses reagents into a solution container from containers in its storage slots.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ReagentDispenserSystem))]
public sealed partial class ReagentDispenserComponent : Component
{
    public const string OutputSlotName = "beakerSlot";

    [DataField]
    public ItemSlot BeakerSlot = new();

    [DataField]
    public SoundSpecifier ClickSound =
        new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));

    [DataField, AutoNetworkedField]
    public FixedPoint2 DispenseAmount = 10;

    // Collection expression is a sandbox violation here. :(
    // ReSharper disable once UseCollectionExpression
    /// <summary>
    /// What amount options to show in the reagent amount selector... thing.
    /// </summary>
    [DataField]
    public List<FixedPoint2> SelectableAmounts = new() { 1, 5, 10, 15, 20, 25, 30, 50, 100 };
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserSetDispenseAmountMessage(FixedPoint2 amount) : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 ReagentDispenserDispenseAmount = amount;
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserDispenseReagentMessage(ItemStorageLocation storageLocation)
    : BoundUserInterfaceMessage
{
    public readonly ItemStorageLocation StorageLocation = storageLocation;
}

/// <summary>
/// Message sent by the user interface to ask the reagent dispenser to eject a container
/// </summary>
[Serializable, NetSerializable]
public sealed class ReagentDispenserEjectContainerMessage(ItemStorageLocation storageLocation)
    : BoundUserInterfaceMessage
{
    public readonly ItemStorageLocation StorageLocation = storageLocation;
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserClearContainerSolutionMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ReagentInventoryItem(
    ItemStorageLocation storageLocation,
    string reagentLabel,
    FixedPoint2 quantity,
    Color reagentColor)
{
    public ItemStorageLocation StorageLocation = storageLocation;
    public string ReagentLabel = reagentLabel;
    public FixedPoint2 Quantity = quantity;
    public Color ReagentColor = reagentColor;
}

[Serializable, NetSerializable]
public sealed class ReagentDispenserBoundUserInterfaceState(
    ContainerInfo? outputContainer,
    NetEntity? outputContainerEntity,
    List<ReagentInventoryItem> inventory,
    List<FixedPoint2> selectableAmounts,
    FixedPoint2 selectedDispenseAmount)
    : BoundUserInterfaceState
{
    public readonly ContainerInfo? OutputContainer = outputContainer;

    public readonly NetEntity? OutputContainerEntity = outputContainerEntity;

    /// <summary>
    /// A list of the reagents which this dispenser can dispense.
    /// </summary>
    public readonly List<ReagentInventoryItem> Inventory = inventory;

    /// <summary>
    /// Corresponds to <see cref="ReagentDispenserComponent.SelectableAmounts"/>.
    /// </summary>
    public readonly List<FixedPoint2> SelectableAmounts = selectableAmounts;

    public readonly FixedPoint2 SelectedDispenseAmount = selectedDispenseAmount;
}

[Serializable, NetSerializable]
public enum ReagentDispenserUiKey { Key }
