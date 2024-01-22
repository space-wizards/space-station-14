using System.Linq;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
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
        HasContainer = true;
    }
}

[Serializable, NetSerializable]
public sealed class MedipenRefillerUpdateState : BoundUserInterfaceState
{
    public List<MedipenRecipePrototype> Recipes;

    public ContainerData InputContainerData;
    public ContainerData BufferData;
    public MedipenRefillerUpdateState(List<MedipenRecipePrototype> recipes, ContainerData input, ContainerData buffer)
    {
        Recipes = recipes;
        InputContainerData = input;
        BufferData = buffer;
    }
}

public enum MedipenRefillerReagentAmount
{
    U1 = 1,
    U5 = 5,
    U10 = 10,
    U25 = 25
}
