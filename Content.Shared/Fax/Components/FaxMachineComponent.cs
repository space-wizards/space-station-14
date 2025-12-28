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
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("name"), AutoNetworkedField]
    public string FaxName { get; set; } = "Unknown";

    /// <summary>
    /// Sprite to use when inserting an object.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public string InsertingState = "inserting";

    /// <summary>
    /// Device address of fax in network to which data will be send
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("destinationAddress"), AutoNetworkedField]
    public string? DestinationFaxAddress { get; set; }

    /// <summary>
    /// Contains the item to be sent, assumes it's paper...
    /// </summary>
    [DataField(required: true), AutoNetworkedField]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Is fax machine should respond to pings in network
    /// This will make it visible to others on the network
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public bool ResponsePings { get; set; } = true;

    /// <summary>
    /// Should admins be notified on message receive
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool NotifyAdmins { get; set; } = false;

    /// <summary>
    /// Should that fax receive nuke codes send by admins. Probably should be captain fax only
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool ReceiveNukeCodes { get; set; } = false;

    /// <summary>
    /// Sound to play when fax performing given action
    /// </summary>
    [DataField]
    public Dictionary<FaxActions, SoundSpecifier> ActionSoundsSpecifiers = new() {
        { FaxActions.Send, new SoundPathSpecifier("/Audio/Machines/high_tech_confirm.ogg") },
        { FaxActions.Print, new SoundPathSpecifier("/Audio/Machines/printer.ogg") },
    };

    /// <summary>
    /// Known faxes in network by address with fax names
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public Dictionary<string, string> KnownFaxes { get; set; } = new();

    /// <summary>
    /// Print queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public Queue<FaxPrintout> PrintingQueue { get; set; } = new();

    /// <summary>
    /// The times at which actions will be complete
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public Dictionary<FaxActions, TimeSpan> ReadyTimes = new();

    /// <summary>
    /// Timeout of each action
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public Dictionary<FaxActions, TimeSpan> ActionTimeout = new() {
        { FaxActions.Print, TimeSpan.FromSeconds(2.3f) },
        { FaxActions.Send, TimeSpan.FromSeconds(5f) },
        { FaxActions.Insert, TimeSpan.FromSeconds(1.88f) }, // 0.02 off for correct animation
    };

    /// <summary>
    /// State of action being performed.
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public Dictionary<FaxActions, bool> IsActionActive = new() {
        { FaxActions.Print, false },
        { FaxActions.Send, false },
        { FaxActions.Insert, false },
    };

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId PrintPaperId = "Paper";

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason of the Office type.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId PrintOfficePaperId = "PaperOffice";

    /// <summary>
    ///     Ongoing sounds of actions fax may be performing
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<FaxActions, EntityUid?> ActionSounds = new();
}

[DataDefinition, Serializable]
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

    private FaxPrintout()
    {
    }

    public FaxPrintout(string content, string name, string? label = null, string? prototypeId = null, string? stampState = null, List<StampDisplayInfo>? stampedBy = null, bool locked = false)
    {
        Content = content;
        Name = name;
        Label = label;
        PrototypeId = prototypeId ?? "";
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
        Locked = locked;
    }
}

public enum FaxActions
{
    Insert,
    Send,
    Print,
}
