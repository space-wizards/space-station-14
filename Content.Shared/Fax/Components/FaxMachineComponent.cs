using Content.Shared.Containers.ItemSlots;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Fax.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FaxMachineComponent : Component
{
    /// <summary>
    /// Name with which the fax will be visible to others on the network
    /// </summary>
    [DataField("name")]
    public string FaxName { get; set; } = "Unknown";

    /// <summary>
    /// Sprite to use when inserting an object.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string InsertingState = "inserting";

    /// <summary>
    /// Device address of fax in network to which data will be send
    /// </summary>
    [DataField("destinationAddress")]
    public string? DestinationFaxAddress { get; set; }

    /// <summary>
    /// Name of fax in network to which data will be send
    /// </summary>
    [DataField("destinationName")]
    public string? DestinationFaxName { get; set; }

    /// <summary>
    /// Contains the item to be sent, assumes it's paper...
    /// </summary>
    [DataField(required: true)]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Is fax machine should respond to pings in network
    /// This will make it visible to others on the network
    /// </summary>
    [DataField]
    public bool ResponsePings { get; set; } = true;

    /// <summary>
    /// Should admins be notified on message receive
    /// </summary>
    [DataField]
    public bool NotifyAdmins { get; set; } = false;

    /// <summary>
    /// Should that fax receive nuke codes send by admins. Probably should be captain fax only
    /// </summary>
    [DataField]
    public bool ReceiveNukeCodes { get; set; } = false;

    /// <summary>
    /// Sound to play when fax printing new message
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound to play when fax successfully send message
    /// </summary>
    [DataField]
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
    [DataField]
    public Queue<FaxPrintout> PrintingQueue { get; private set; } = new();

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public float SendTimeoutRemaining;

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public float SendTimeout = 5f;

    /// <summary>
    /// Remaining time of inserting animation
    /// </summary>
    [DataField]
    public float InsertingTimeRemaining;

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [ViewVariables]
    public float InsertionTime = 1.88f; // 0.02 off for correct animation

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField]
    public float PrintingTimeRemaining;

    /// <summary>
    /// How long the printing animation will play
    /// </summary>
    [ViewVariables]
    public float PrintingTime = 2.3f;

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason.
    /// </summary>
    [DataField]
    public EntProtoId PrintPaperId = "Paper";

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason of the Office type.
    /// </summary>
    [DataField]
    public EntProtoId PrintOfficePaperId = "PaperOffice";

    /// <summary>
    ///     If the fax machine should add a bit of text in the end of the fax that specifies from where and to where the fax is for
    /// </summary>
    [DataField]
    public bool AddSenderInfo = true;

    /// <summary>
    ///     The text that is sent along with the paper's content if <see cref="AddSenderInfo"/> is true
    /// </summary>
    [DataField]
    public LocId SenderInfo = "fax-machine-sender-info";
}

[DataDefinition]
public sealed partial class FaxPrintout
{
    [DataField(required: true)]
    public string Name { get; private set; } = default!;

    [DataField]
    public string? Label { get; private set; }

    [DataField(required: true)]
    public string Content { get; private set; } = default!;

    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string PrototypeId { get; private set; } = default!;

    [DataField("stampState")]
    public string? StampState { get; private set; }

    [DataField("stampedBy")]
    public List<StampDisplayInfo> StampedBy { get; private set; } = new();

    [DataField]
    public bool Locked { get; private set; }

    [DataField]
    public string? SenderFaxName { get; private set; } = default!;

    private FaxPrintout()
    {
    }

    public FaxPrintout(string content, string name, string? label = null, string? prototypeId = null, string? stampState = null, List<StampDisplayInfo>? stampedBy = null, bool locked = false, string? senderFaxName = null)
    {
        Content = content;
        Name = name;
        Label = label;
        PrototypeId = prototypeId ?? "";
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
        Locked = locked;
        SenderFaxName = senderFaxName;
    }
}
