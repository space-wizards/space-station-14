using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Fax;

[RegisterComponent]
public sealed class FaxMachineComponent : Component
{
    /**
     * Visible to other name of current fax in network
     */
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("name")]
    public string FaxName { get; set; } = "Unknown";

    /**
     * Device address of fax in network to which paper will be send
     */
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("destinationAddress")]
    public string? DestinationFaxAddress { get; set; }

    /**
     * Contains the item to be send, assumes it's paper...
     */
    [DataField("paperSlot")]
    public ItemSlot PaperSlot = new();

    /**
     * Is fax machine should be visible to other fax machines in network
     */
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isVisibleInNetwork")]
    public bool IsVisibleInNetwork { get; set; } = true;
    
    /*
     * Known faxes in network by address with fax names
     */
    [ViewVariables]
    public Dictionary<string, string> KnownFaxes { get; } = new();

    /*
     * History records of sent and received fax messages
     */
    [ViewVariables]
    public List<HistoryRecord> History { get; } = new();
}

/// <summary>
/// Fax sent/received history record
/// </summary>
public sealed class HistoryRecord
{
    [ViewVariables]
    public string Type { get; set; } // TODO: Make enum or something simple

    [ViewVariables]
    public string FaxName { get; set; }

    [ViewVariables]
    public string Time { get; set; }

    public HistoryRecord(string type, string faxName, string time)
    {
        Type = type;
        FaxName = faxName;
        Time = time;
    }
}

