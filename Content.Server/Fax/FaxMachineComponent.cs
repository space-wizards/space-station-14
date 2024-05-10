using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Fax;

[RegisterComponent]
public sealed partial class FaxMachineComponent : Component
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
    /// Contains the item to be sent, assumes it's paper...
    /// </summary>
    [DataField("paperSlot", required: true)]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Is fax machine should respond to pings in network
    /// This will make it visible to others on the network
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("responsePings")]
    public bool ResponsePings { get; set; } = true;

    /// <summary>
    /// Should admins be notified on message receive
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("notifyAdmins")]
    public bool NotifyAdmins { get; set; } = false;

    /// <summary>
    /// Should that fax receive nuke codes send by admins. Probably should be captain fax only
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("receiveNukeCodes")]
    public bool ReceiveNukeCodes { get; set; } = false;

    /// <summary>
    /// Sound to play when fax has been emagged
    /// </summary>
    [DataField("emagSound")]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Sound to play when fax printing new message
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound to play when fax successfully send message
    /// </summary>
    [DataField("sendSound")]
    public SoundSpecifier SendSound = new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg");

    /// <summary>
    /// Known faxes in network by address with fax names
    /// </summary>
    [ViewVariables]
    public Dictionary<string, string> KnownFaxes { get; } = new();

    /// <summary>
    /// Print queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField("printingQueue")]
    public Queue<FaxPrintout> PrintingQueue { get; private set; } = new();

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField("sendTimeoutRemaining")]
    public float SendTimeoutRemaining;

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField("sendTimeout")]
    public float SendTimeout = 5f;

    /// <summary>
    /// Remaining time of inserting animation
    /// </summary>
    [DataField("insertingTimeRemaining")]
    public float InsertingTimeRemaining;

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [ViewVariables]
    public float InsertionTime = 1.88f; // 0.02 off for correct animation

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField("printingTimeRemaining")]
    public float PrintingTimeRemaining;

    /// <summary>
    /// How long the printing animation will play
    /// </summary>
    [ViewVariables]
    public float PrintingTime = 2.3f;
}

[DataDefinition]
public sealed partial class FaxPrintout
{
    [DataField("name", required: true)]
    public string Name { get; private set; } = default!;

    [DataField("content", required: true)]
    public string Content { get; private set; } = default!;

    [DataField("prototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string PrototypeId { get; private set; } = default!;

    [DataField("stampState")]
    public string? StampState { get; private set; }

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; private set; } = new();

    private FaxPrintout()
    {
    }

    public FaxPrintout(string content, string name, string? prototypeId = null, string? stampState = null, List<StampDisplayInfo>? stampedBy = null)
    {
        Content = content;
        Name = name;
        PrototypeId = prototypeId ?? "";
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
    }
}
