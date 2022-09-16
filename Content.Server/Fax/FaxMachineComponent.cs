using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Fax;

[RegisterComponent]
public sealed class FaxMachineComponent : Component
{
    /**
     * Visible to other id of current fax in network
     */
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("id")]
    public string FaxId { get; set; } = "fax";

    /**
     * Id of fax in network to which paper will be send
     */
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("destinationId")]
    public string? DestinationFaxId { get; set; }

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
    
    // Known faxes in network by address with faxId
    public Dictionary<string, string> KnownFaxes { get; } = new();
}
