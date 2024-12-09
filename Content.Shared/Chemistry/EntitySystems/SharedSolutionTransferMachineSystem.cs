using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry
{
    public sealed class SharedSolutionTransferMachineSystem
    {
        public const string BaseStorageSlotId = "SolutionTransferMachine-storageSlot";
        public const string BaseDispenserSlotId = "SolutionTransferMachine-dispenserSlot";
    }

    public enum SolutionTransferMachineRestriction
    {
        Unrestricted = 0,
        IntoStorage = 1 << 0,
        IntoDispenser = 1 << 1,
        FromStorage = 1 << 2,
        FromDispenser = 1 << 3,
        StoragePicking = 1 << 4,
        DispenserPicking = 1 << 5,
    }

    [Serializable, NetSerializable]
    public sealed class ReagentInventoryItem(string storageSlotId, string reagentLabel, Color reagentColor, FixedPoint2 quantity, FixedPoint2? capacity, List<ReagentQuantity>? reagents, NetEntity? entity)
    {
        public readonly string StorageSlotId = storageSlotId;
        public readonly string DisplayName = reagentLabel;
        public readonly FixedPoint2 Quantity = quantity;
        public readonly FixedPoint2? Capacity = capacity;
        public readonly Color ReagentColor = reagentColor;
        public List<ReagentQuantity>? Reagents { get; init; } = reagents;
        public readonly NetEntity? Entity = entity;
    }
}