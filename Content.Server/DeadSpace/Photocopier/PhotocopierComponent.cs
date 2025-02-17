// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeadSpace.Photocopier;
using Content.Shared.Paper;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.DeadSpace.Photocopier;

[RegisterComponent]
public sealed partial class PhotocopierComponent : Component
{
    /// <summary>
    /// Contains the item to be photocopiered, assumes it's paper...
    /// </summary>
    [DataField(required: true)]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Sound to play when photocopier has been emagged
    /// </summary>
    [DataField]
    public SoundSpecifier EmagSound = new SoundCollectionSpecifier("sparks");

    /// <summary>
    /// Was photocopier has been emagged. If emagged, will show the syndicate paperwork
    /// </summary>
    [DataField]
    public bool WasEmagged = false;

    /// <summary>
    /// Sound to play when photocopier printing
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundPathSpecifier("/Audio/Machines/printer.ogg");

    /// <summary>
    /// Sound to play when photocopier scans
    /// </summary>
    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/_DeadSpace/Machines/scanning.ogg");

    /// <summary>
    /// Print queue
    /// </summary>
    [ViewVariables]
    [DataField]
    public Queue<PhotocopierPrintout> PrintingQueue { get; private set; } = new();

    /// <summary>
    /// Reuse timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public float UseTimeoutRemaining;

    /// <summary>
    /// Use timeout
    /// </summary>
    [ViewVariables]
    [DataField]
    public float UseTimeout = 2.6f;

    /// <summary>
    /// Remaining time of scanning animation
    /// </summary>
    [DataField]
    public float ScanningTimeRemaining;

    /// <summary>
    /// How long the scanning animation will play
    /// </summary>
    [ViewVariables]
    public float ScanningTime = 12.1f;

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

    [ViewVariables]
    public PhotocopierMode Mode = PhotocopierMode.Copy;

    /// <summary>
    /// The type of photocopier. It depends on which paperwork photocopier will show by default
    /// </summary>
    [DataField]
    public PhotocopierType PhotocopierType = PhotocopierType.Default;

    /// <summary>
    /// Chosen paper form than will be printed
    /// </summary>
    [DataField]
    public PaperworkFormPrototype? ChosenPaper = null;

    [ViewVariables]
    public int TonerLeft = 30;

    [ViewVariables]
    public int MaxTonerAmount = 30;

    [DataField]
    public SoundSpecifier TonerRestock = new SoundPathSpecifier("/Audio/_DeadSpace/Machines/vending_restock_done_cuted.ogg");
}

[DataDefinition]
public sealed partial class PhotocopierPrintout
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

    public PhotocopierPrintout(string content, string name, string prototypeId, string? stampState = null, List<StampDisplayInfo>? stampedBy = null)
    {
        Content = content;
        Name = name;
        PrototypeId = prototypeId;
        StampState = stampState;
        StampedBy = stampedBy ?? new List<StampDisplayInfo>();
    }
}
