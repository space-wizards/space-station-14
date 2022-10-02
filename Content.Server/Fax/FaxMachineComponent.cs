using Content.Shared.Containers.ItemSlots;
using Robust.Shared.Audio;

namespace Content.Server.Fax;

[RegisterComponent]
public sealed class FaxMachineComponent : Component
{
    /// <summary>
    /// Name with which the fax will be visible to others on the network
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("name")]
    public string FaxName { get; set; } = "Unknown";

    /// <summary>
    /// Device address of fax in network to which data will be send
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
    /// Is fax machine should respond to pings in network
    /// This will make it visible to others on the network
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("shouldResponsePings")]
    public bool ShouldResponsePings { get; set; } = true;
    
    /// <summary>
    /// Is fax was emaaged
    /// </summary>
    [ViewVariables]
    [DataField("emagged")]
    public bool Emagged { get; set; } = false;

    /// <summary>
    /// Sound to play when fax has been emagged
    /// </summary>
    [DataField("emagSound")]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Known faxes in network by address with fax names
    /// </summary>
    [ViewVariables]
    public Dictionary<string, string> KnownFaxes { get; } = new();

    /// <summary>
    /// Print queue of the incoming message
    /// </summary>
    [ViewVariables]
    public Queue<string> PrintingQueue { get; } = new();

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
