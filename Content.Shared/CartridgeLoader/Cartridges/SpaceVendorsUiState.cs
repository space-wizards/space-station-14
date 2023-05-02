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
    public int Minutes = 0;

    private readonly DateTime _dateTimeCreation;

    public AppraisedItem(string name, string price)
    {
        Name = name;
        Price = price;
        _dateTimeCreation = DateTime.Now;
    }

    public void UpdateElapsedTimeData()
    {
        TimeSpan date = DateTime.Now - _dateTimeCreation;
        Minutes = date.Hours * 60 + date.Minutes;
    }
}
