using Content.Shared.Cloning;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceNetwork;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Fax.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true, true), AutoGenerateComponentPause]
public sealed partial class FaxMachineComponent : Component
{
    /// <summary>
    /// Current functions this fax machine is performing.
    /// </summary>
    [DataField]
    [AutoNetworkedField]
    public FaxFunctions Functions;

    /// <summary>
    /// The cloning settings for this fax machine.
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> Settings = "Paper";

    /// <summary>
    /// Items which we allow to be faxed.
    /// If null, we allow all items.
    /// Anything not whitelisted, we deal damage to.
    /// </summary>
    [DataField]
    public EntityWhitelist? Whitelist = new ()
    {
        Components =
        [
            "Paper",
        ],
    };

    /// <summary>
    /// Name with which the fax will be visible to others on the network
    /// </summary>
    [DataField("name")]
    public string FaxName { get; set; } = "Unknown";

    /// <summary>
    /// Device address of fax in network to which data will be send
    /// </summary>
    [DataField("destinationAddress"), AutoNetworkedField]
    public string? DestinationFaxAddress { get; set; }

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
    public bool NotifyAdmins { get; set; }

    /// <summary>
    /// Should that fax receive nuke codes send by admins. Probably should be captain fax only
    /// </summary>
    [DataField]
    public bool ReceiveNukeCodes { get; set; }

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
    /// Key is the fax address, Value is the fax name.
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public Dictionary<string, string> KnownFaxes { get; set; } = new();

    /// <summary>
    /// Print queue of the incoming message
    /// </summary>
    [ViewVariables]
    [DataField, AutoNetworkedField]
    public Queue<FaxPayload> PrintingQueue { get; set; } = new();

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextPrintTime;

    /// <summary>
    /// Message sending timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public TimeSpan InteractionTimeout = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// Remaining time of inserting animation
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan InsertionEnd;

    /// <summary>
    /// How long the inserting animation will play
    /// </summary>
    [ViewVariables]
    public TimeSpan InsertionTime = TimeSpan.FromSeconds(2.4f);

    /// <summary>
    /// Remaining time of printing animation
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan PrintTimeEnd;

    /// <summary>
    /// How long the printing animation will play
    /// </summary>
    [ViewVariables]
    public TimeSpan PrintingTime = TimeSpan.FromSeconds(3.0f);

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason.
    /// </summary>
    [DataField]
    public EntProtoId PrintPaperId = FaxSystem.PaperId;

    /// <summary>
    ///     The prototype ID to use for faxed or copied entities if we can't get one from
    ///     the paper entity for whatever reason of the Office type.
    /// </summary>
    [DataField]
    public EntProtoId PrintOfficePaperId = FaxSystem.OfficePaperId;

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

[Flags]
[Serializable, NetSerializable]
public enum FaxFunctions : byte
{
    /// <summary>
    /// Fax doing nothing
    /// </summary>
    Idle = 0,

    /// <summary>
    /// Fax is printing
    /// </summary>
    Printing = 1 << 0,

    /// <summary>
    /// Fax is inserting paper
    /// </summary>
    Inserting = 1 << 1,

    /// <summary>
    /// Fax is on cooldown from having queued up an entity to send, copy, or print
    /// </summary>
    Processing = 1 << 2
}

/// <summary>
/// Data for a fax printout
/// </summary>
[DataDefinition]
[Serializable, NetSerializable]
public readonly partial record struct FaxPayload(NetEntity Printout, string? Sender = null) : INetworkPayload
{
    /// <summary>
    /// Entity being faxed.
    /// </summary>
    [DataField(required: true)]
    public readonly NetEntity Printout = Printout;

    /// <summary>
    /// Name of the fax sending the entity.
    /// </summary>
    [DataField]
    public readonly string? SenderName = Sender;
}
