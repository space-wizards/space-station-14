using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Chemistry;

public sealed class SharedMedipenRefiller
{
    public const string BufferSolutionName = "buffer";
    public const string InputSlotName = "beakerSlot";
    public const string MedipenSlotName = "medipenSlot";
    public const string MedipenSolutionName = "pen";

    [Serializable, NetSerializable]
    public enum MedipenRefillerUiKey
    {
        Key
    }

    public static bool CanRefill(string id, List<MedipenRecipePrototype> recipes, List<ReagentQuantity> content, IPrototypeManager prototypeManager, bool isInserted)
    {
        if (!isInserted)
            return false;

        var reagents = new Dictionary<string, FixedPoint2>();
        var requiredReagents = new Dictionary<string, FixedPoint2>();

        foreach (var recipe in recipes!)
        {
            if (recipe.ID.Equals(id))
            {
                requiredReagents = recipe.RequiredReagents;
                foreach (var reagent in content)
                {
                    if (prototypeManager.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var reagentProto))
                        reagents.Add(reagentProto.ID, reagent.Quantity);
                }
            }
        }

        if (reagents.Count.Equals(requiredReagents.Count))
        {
            foreach (var reagent in reagents)
            {
                if (!(requiredReagents.ContainsKey(reagent.Key) && requiredReagents[reagent.Key].Equals(reagent.Value)))
                    return false;
            }
            return true;
        }
        else
            return false;
    }
}

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
    /// If there's a item loaded.
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
    public List<MedipenRecipePrototype> Recipes;
    public ContainerData InputContainerData;
    public ContainerData BufferData;
    public bool IsActivated;
    public string CurrentRecipe;
    public int RemainingTime;
    public MedipenRefillerUpdateState(List<MedipenRecipePrototype> recipes, ContainerData input, ContainerData buffer, bool isActivated, string currentRecipe, int remainingTime)
    {
        Recipes = recipes;
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
