using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

/// <summary>
/// Stores information about the containers, which will be sent to the client. Basically the same as <see cref="ContainerInfo"/>.
/// </summary>
[NetSerializable, Serializable]
public sealed class ContainerData
{
    public string? Name = null;
    /// <summary>
    /// A list with container reagents data.
    /// </summary>
    public List<ReagentQuantity> ReagentQuantities = new();
    /// <summary>
    /// The volume of the container.
    /// </summary>
    public FixedPoint2 CurrentVolume = 0;
    /// <summary>
    /// The total volume of reagents.
    /// </summary>

    public FixedPoint2 TotalVolume = 0;
    /// <summary>
    /// If there's a item loaded. If it's the input, it refers to the container, if it's the buffer, it refers to medipen.
    /// </summary>
    public bool HasContainer = false;

    public ContainerData()
    {

    }
    public ContainerData(string name, List<ReagentQuantity> reagentQuantities, FixedPoint2 currentVolume, FixedPoint2 totalVolume, bool hasContainer)
    {
        Name = name;
        ReagentQuantities = reagentQuantities;
        CurrentVolume = currentVolume;
        TotalVolume = totalVolume;
        HasContainer = hasContainer;
    }
}

[Serializable, NetSerializable]
public sealed class MedipenRefillerUpdateState : BoundUserInterfaceState
{
    public ContainerData InputContainerData;
    public ContainerData BufferData;
    public bool IsActivated;
    public string CurrentRecipe;
    public int RemainingTime;
    public MedipenRefillerUpdateState(ContainerData input, ContainerData buffer,
                                      bool isActivated, string currentRecipe, int remainingTime)
    {
        InputContainerData = input;
        BufferData = buffer;
        IsActivated = isActivated;
        CurrentRecipe = currentRecipe;
        RemainingTime = remainingTime;
    }
}

[Serializable, NetSerializable]
public sealed class MedipenRefillerTransferReagentMessage : BoundUserInterfaceMessage
{
    public ReagentId Id;
    public FixedPoint2 Amount;
    public bool IsBuffer;

    public MedipenRefillerTransferReagentMessage(ReagentId id, FixedPoint2 amount, bool isBuffer)
    {
        Id = id;
        Amount = amount;
        IsBuffer = isBuffer;
    }
}

[Serializable, NetSerializable]
public sealed class MedipenRefillerActivateMessage : BoundUserInterfaceMessage
{
    public MedipenRecipePrototype MedipenRecipe;
    public MedipenRefillerActivateMessage(MedipenRecipePrototype medipenRecipe)
    {
        MedipenRecipe = medipenRecipe;
    }
}
