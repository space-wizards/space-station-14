using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

/// <summary>
/// This class holds constants that are shared between client and server.
/// </summary>
public sealed class ChemMasterConstants
{
    public const uint PillTypes = 20;
    public const string BufferSolutionName = "buffer";
    public const string InputSlotName = "beakerSlot";
    public const string OutputSlotName = "outputSlot";
    public const string PillSolutionName = "food";
    public const string BottleSolutionName = "drink";
    public const uint LabelMaxLength = 50;
}

[Serializable, NetSerializable]
public sealed class ChemMasterSetModeMessage(ChemMasterMode mode) : BoundUserInterfaceMessage
{
    public readonly ChemMasterMode ChemMasterMode = mode;
}

[Serializable, NetSerializable]
public sealed class ChemMasterSetPillTypeMessage(uint pillType) : BoundUserInterfaceMessage
{
    public readonly uint PillType = pillType;
}

[Serializable, NetSerializable]
public sealed class ChemMasterReagentAmountButtonMessage(
    ReagentId reagentId,
    ChemMasterReagentAmount amount,
    bool fromBuffer)
    : BoundUserInterfaceMessage
{
    public readonly ReagentId ReagentId = reagentId;
    public readonly ChemMasterReagentAmount Amount = amount;
    public readonly bool FromBuffer = fromBuffer;
}

[Serializable, NetSerializable]
public sealed class ChemMasterCreatePillsMessage(uint dosage, uint number, string label) : BoundUserInterfaceMessage
{
    public readonly uint Dosage = dosage;
    public readonly uint Number = number;
    public readonly string Label = label;
}

[Serializable, NetSerializable]
public sealed class ChemMasterOutputToBottleMessage(uint dosage, string label) : BoundUserInterfaceMessage
{
    public readonly uint Dosage = dosage;
    public readonly string Label = label;
}

[Serializable, NetSerializable]
public sealed class ChemMasterOutputDrawSourceMessage(ChemMasterDrawSource drawSource) : BoundUserInterfaceMessage
{
    public readonly ChemMasterDrawSource DrawSource = drawSource;
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

// TODO: fix this implementation
public enum ChemMasterReagentAmount
{
    U1 = 1,
    U5 = 5,
    U10 = 10,
    U15 = 15,
    U20 = 20,
    U30 = 30,
    U40 = 40,
    U60 = 60,
    U120 = 120,
    All,
}

public enum ChemMasterDrawSource
{
    Internal,
    External,
}

public static class ChemMasterReagentAmountToFixedPoint
{
    public static FixedPoint2 GetFixedPoint(this ChemMasterReagentAmount amount)
    {
        return amount == ChemMasterReagentAmount.All ? FixedPoint2.MaxValue : FixedPoint2.New((int)amount);
    }
}

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
    public List<(string Id, FixedPoint2 Quantity)>? Entities { get; init; }

    public List<ReagentQuantity>? Reagents { get; init; }
}

[Serializable, NetSerializable]
public enum ChemMasterUiKey : byte
{
    Key
}
