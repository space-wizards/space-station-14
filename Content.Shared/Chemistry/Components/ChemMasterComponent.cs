using System.Globalization;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry.Components;

/// <summary>
/// An industrial grade chemical manipulator with pill and bottle production included.
/// </summary>
/// <seealso cref="ChemMasterSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ChemMasterSystem))]
public sealed partial class ChemMasterComponent : Component
{
    public const uint PillTypes = 20;
    public const uint LabelMaxLength = 50;
    public const string BottleSolutionName = "drink";
    public const string BufferSolutionName = "buffer";
    public const string PillSolutionName = "food";
    public const string InputSlotName = "beakerSlot";
    public const string OutputSlotName = "outputSlot";

    // ReSharper disable once UseCollectionExpression
    public static readonly List<FixedPoint2> ChemMasterAmountOptions =
        new() { 1, 5, 10, 15, 20, 25, 30, 50, 100, FixedPoint2.MaxValue };

    [DataField, AutoNetworkedField]
    public uint PillType = 0;

    [DataField, AutoNetworkedField]
    public ChemMasterMode Mode = ChemMasterMode.Transfer;

    [DataField, AutoNetworkedField]
    public ChemMasterSortingType SortingType = ChemMasterSortingType.None;

    [DataField(required: true)]
    public FixedPoint2 PillDosageLimit;

    [DataField]
    public SoundSpecifier ClickSound =
        new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg", AudioParams.Default.WithVolume(-2f));
}

[Serializable, NetSerializable]
public sealed class ChemMasterSetModeMessage : BoundUserInterfaceMessage
{
    public readonly ChemMasterMode ChemMasterMode;

    public ChemMasterSetModeMessage(ChemMasterMode mode)
    {
        ChemMasterMode = mode;
    }
}

[Serializable, NetSerializable]
public sealed class ChemMasterSetPillTypeMessage(uint pillType) : BoundUserInterfaceMessage
{
    public readonly uint PillType = pillType;
}

[Serializable, NetSerializable]
public sealed class ChemMasterReagentAmountButtonMessage(ReagentId reagentId, FixedPoint2 amount, bool fromBuffer)
    : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId = reagentId;
    public readonly FixedPoint2 Amount = amount;
    public readonly bool FromBuffer = fromBuffer;
}

[Serializable, NetSerializable]
public sealed class ChemMasterCreatePillsMessage(FixedPoint2 dosage, uint number, string label)
    : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Dosage = dosage;
    public readonly uint Number = number;
    public readonly string Label = label;
}

[Serializable, NetSerializable]
public sealed class ChemMasterOutputToBottleMessage(FixedPoint2 dosage, string label) : BoundUserInterfaceMessage
{
    public readonly FixedPoint2 Dosage = dosage;
    public readonly string Label = label;
}

public enum ChemMasterMode
{
    Transfer,
    Discard,
}

public enum ChemMasterSortingType : byte
{
    None = 0,
    Alphabetical = 1,
    Quantity = 2,
    Latest = 3,
}

[Serializable, NetSerializable]
public sealed class ChemMasterSortingTypeCycleMessage : BoundUserInterfaceMessage;

/// <summary>
/// Information about the capacity and contents of a container for display in the UI
/// </summary>
[Serializable, NetSerializable]
public sealed class ContainerInfo(string displayName, FixedPoint2 currentVolume, FixedPoint2 maxVolume)
{
    /// <summary>
    /// The container name to show to the player
    /// </summary>
    public readonly string DisplayName = displayName;

    /// <summary>
    /// The currently used volume of the container
    /// </summary>
    public readonly FixedPoint2 CurrentVolume = currentVolume;

    /// <summary>
    /// The maximum volume of the container
    /// </summary>
    public readonly FixedPoint2 MaxVolume = maxVolume;

    /// <summary>
    /// A list of the entities and their sizes within the container
    /// </summary>
    public List<(NetEntity Id, FixedPoint2 Quantity)>? Entities { get; init; }

    public List<ReagentQuantity>? Reagents { get; init; }

    /// <summary>
    /// Returns the localized current versus max volume of the container (e.g., 50/100).
    /// </summary>
    /// <remarks>
    /// I kinda wish it was 50u/100u but out of convention I won't change it.
    /// </remarks>
    public string LocalizedCapacity()
    {
        return Loc.GetString("reagent-container-available-capacity",
            ("currentVolume", CurrentVolume.ToString(CultureInfo.CurrentCulture)),
            ("maxVolume", MaxVolume.ToString(CultureInfo.CurrentCulture)));
    }
}

// TODO this needs distilled down into what's actually used and can be derived from other arguments.
// There's no way all of this is needed.
[Serializable, NetSerializable]
public sealed class ChemMasterBoundUserInterfaceState(
    ChemMasterMode mode,
    ChemMasterSortingType sortingType,
    ContainerInfo? inputContainerInfo,
    ContainerInfo? outputContainerInfo,
    IReadOnlyList<ReagentQuantity> bufferReagents,
    FixedPoint2 bufferCurrentVolume,
    uint selectedPillType,
    FixedPoint2 pillDosageLimit,
    bool updateLabel)
    : BoundUserInterfaceState
{
    public readonly ContainerInfo? InputContainerInfo = inputContainerInfo;
    public readonly ContainerInfo? OutputContainerInfo = outputContainerInfo;

    /// <summary>
    /// A list of the reagents and their amounts within the buffer, if applicable.
    /// </summary>
    public readonly IReadOnlyList<ReagentQuantity> BufferReagents = bufferReagents;

    public readonly ChemMasterMode Mode = mode;

    public readonly ChemMasterSortingType SortingType = sortingType;

    public readonly FixedPoint2? BufferCurrentVolume = bufferCurrentVolume;
    public readonly uint SelectedPillType = selectedPillType;

    public readonly FixedPoint2 PillDosageLimit = pillDosageLimit;

    public readonly bool UpdateLabel = updateLabel;
}

[Serializable, NetSerializable]
public enum ChemMasterUiKey { Key }
