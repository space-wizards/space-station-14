using Robust.Shared.Serialization;

namespace Content.Shared.CartridgeLoader.Cartridges;

[Serializable, NetSerializable]
public sealed class SpaceVendorsUiState : BoundUserInterfaceState
{
    public List<AppraisedItem> AppraisedItems;

    public SpaceVendorsUiState(List<AppraisedItem> appraisedItems)
    {
        AppraisedItems = appraisedItems;
    }
}

[Serializable, NetSerializable, DataRecord]
public sealed class AppraisedItem
{
    public readonly string Name;
    public readonly string Price;
    public readonly DateTime DateTimeCreation;

    public AppraisedItem(string name, string price)
    {
        Name = name;
        Price = price;
        DateTimeCreation = DateTime.Now;
    }
}
