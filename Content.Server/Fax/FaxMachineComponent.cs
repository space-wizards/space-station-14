using Content.Shared.Containers.ItemSlots;

namespace Content.Server.Fax;

[RegisterComponent]
public sealed class FaxMachineComponent : Component
{
    /// <summary>
    /// Visible to other name of current fax in network
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("name")]
    public string FaxName { get; set; } = "Unknown";

    /// <summary>
    /// Device address of fax in network to which paper will be send
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("destinationAddress")]
    public string? DestinationFaxAddress { get; set; }

    /// <summary>
    /// Contains the item to be send, assumes it's paper...
    /// </summary>
    [DataField("paperSlot")]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Is fax machine should be visible to other fax machines in network
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("isVisibleInNetwork")]
    public bool IsVisibleInNetwork { get; set; } = true;

    /// <summary>
    /// Known faxes in network by address with fax names
    /// </summary>
    [ViewVariables]
    public Dictionary<string, string> KnownFaxes { get; } = new();

    /// <summary>
    /// Buffer of printing text
    /// </summary>
    [ViewVariables]
    public string? TextBuffer;

    /// <summary>
    /// Remain time of inserting animation
    /// </summary>
    [DataField("insertingTimeRemaining")]
    public float InsertingTimeRemaining;

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [ViewVariables]
    public float InsertionTime = 1.88f; // 0.02 off for correct animation

    /// <summary>
    /// Remain time of printing animation
    /// </summary>
    [DataField("printingTimeRemaining")]
    public float PrintingTimeRemaining;

    /// <summary>
    /// How long the printing animation will play
    /// </summary>
    [ViewVariables]
    public float PrintingTime = 2.3f;
}
