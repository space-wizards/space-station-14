// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Containers.ItemSlots;
using Content.Shared.Damage;
using Content.Shared.Humanoid;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;

namespace Content.Shared.SS220.Photocopier;

[RegisterComponent, NetworkedComponent]
public sealed partial class PhotocopierComponent : Component
{
    // ReSharper disable RedundantLinebreak

    public const string PaperSlotId = "CopierScan";
    public const string TonerSlotId = "TonerCartridge";

    /// <summary>
    /// Minimal time interval between attempts to manually cause photocopier to burn someone's butt.
    /// Also is the manual butt burn animation duration.
    /// </summary>
    [DataField("manualButtBurnDuration")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ManualButtBurnDuration = 1.0f;

    /// <summary>
    /// Used by the server to determine how long the photocopier stays in the "Printing" state.
    /// </summary>
    [DataField("printingTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float PrintingTime = 2.0f;

    /// <summary>
    /// Sound that plays when inserting paper.
    /// Whether it plays or not depends on power availability.
    /// </summary>
    [DataField("paperInsertSound")]
    public SoundSpecifier PaperInsertSound =
        new SoundPathSpecifier("/Audio/Machines/scanning.ogg")
        {
            Params = new AudioParams
            {
                Volume = 0f
            }
        };

    /// <summary>
    /// Sound that plays when printing
    /// </summary>
    [DataField("printSound")]
    public SoundSpecifier PrintSound =
        new SoundPathSpecifier("/Audio/Machines/printer.ogg")
        {
            Params = new AudioParams
            {
                Volume = -2f
            }
        };

    /// <summary>
    /// Sound that plays when a hacked photocopier burns someones butt
    /// </summary>
    [DataField("buttDamageSound")]
    public SoundSpecifier ButtDamageSound =
        new SoundPathSpecifier("/Audio/Items/welder2.ogg")
        {
            Params = new AudioParams
            {
                Volume = -4f
            }
        };

    /// <summary>
    /// Contains an item to be copied, assumes it's paper
    /// </summary>
    [DataField("paperSlot", required: true)]
    public ItemSlot PaperSlot = new();

    /// <summary>
    /// Contains a toner cartridge
    /// </summary>
    [DataField("tonerSlot", required: true)]
    public ItemSlot TonerSlot = new();

    /// <summary>
    /// Collections of forms available in UI
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("formCollections")]
    public HashSet<string> FormCollections = new();

    /// <summary>
    /// Maximum amount of copies that can be queued
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxQueueLength")]
    public int MaxQueueLength
    {
        get => _maxQueueLength;
        set
        {
            if (value < 1)
                throw new Exception("MaxQueueLength can't be less than 1.");

            _maxQueueLength = value;
        }
    }
    private int _maxQueueLength = 10;

    /// <summary>
    /// Damage dealt to a creature when they try to photocopy their butt on a hacked photocopier.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("buttDamage")]
    public DamageSpecifier? ButtDamage;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("contrabandFormCollections")]
    public HashSet<string> ContrabandFormCollections = new();

    /// STATE

    /// <summary>
    /// Used by photocopier to determine whether the species on top of the photocopier is the same as it was
    /// without having to fetch the texture every tick.
    /// </summary>
    [ViewVariables]
    public string? ButtSpecies;

    /// <summary>
    /// Whether this photocopier currently burns butts or not. Set by WireAction.
    /// </summary>
    [DataField("burnsButts")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool BurnsButts = false;

    /// <summary>
    /// Whether this photocopier currently provides contraband forms or not. Set by WireAction.
    /// </summary>
    [DataField("susFormsUnlocked")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool SusFormsUnlocked = false;

    /// <summary>
    /// Contains fields of components that will be copied.
    /// Is applied to a new entity that is created as a result of photocopying.
    /// </summary>
    [ViewVariables]
    public Dictionary<Type, IPhotocopiedComponentData>? DataToCopy;

    /// <summary>
    /// Contains metadata that will be copied.
    /// Is applied to a new entity that is created as a result of photocopying.
    /// </summary>
    public PhotocopyableMetaData? MetaDataToCopy;

    /// <summary>
    /// An audio stream of printing sound.
    /// Is saved in a variable so sound can be stopped later.
    /// </summary>
    public EntityUid? PrintAudioStream;

    [ViewVariables(VVAccess.ReadOnly)]
    public PhotocopierState State = PhotocopierState.Idle;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? EntityOnTop;

    public HumanoidAppearanceComponent? HumanoidAppearanceOnTop;

    /// <summary>
    /// Remaining time of printing
    /// </summary>
    public float PrintingTimeRemaining;

    /// <summary>
    /// Remaining amount of copies to print
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public int CopiesQueued;

    [ViewVariables(VVAccess.ReadOnly)]
    public bool IsCopyingPhysicalButt;

    [ViewVariables(VVAccess.ReadOnly)]
    public float? ManualButtBurnAnimationRemainingTime;
}

