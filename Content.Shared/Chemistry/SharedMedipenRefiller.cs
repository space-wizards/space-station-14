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
    /// <summary>
    /// A list with container reagents data.
    /// </summary>
    public List<ReagentQuantity> ReagentQuantities;
    /// <summary>
    /// The volume of the container.
    /// </summary>
    public FixedPoint2 CurrentVolume;
    /// <summary>
    /// The total volume of reagents.
    /// </summary>

    public FixedPoint2 TotalVolume;
    /// <summary>
    /// If there's a item loaded.
    /// </summary>
    public bool HasContainer;
    public string Name;


    public ContainerData(List<ReagentQuantity> reagentQuantities, FixedPoint2 currentVolume, FixedPoint2 totalVolume, bool hasContainer = false, string name = "")
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
