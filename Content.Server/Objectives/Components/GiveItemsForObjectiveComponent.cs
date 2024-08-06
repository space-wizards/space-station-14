using Content.Server.Objectives.Systems;
using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Server.Objectives.Components;

/// <summary>
///     Will give an item to the player when assigned the objective.
///     Can check if the player has space and by default will cancel the objective if they don't.
/// </summary>
[RegisterComponent, Access(typeof(GiveItemsForObjectiveSystem))]
public sealed partial class GiveItemsForObjectiveComponent : Component
{
    /// <summary>
    ///     Prototype of the item being spawned.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<WeightedRandomPrototype> ItemsToSpawn;

    /// <summary>
    ///     If true, will cancel the assigment of the objective if the item can't be fit into the players backpack.
    ///     If false, the item will be spawned on the ground if the backpack is full.
    /// </summary>
    [DataField]
    public bool CancelAssignmentOnNoSpace = true;
}

/// <summary>
///     Simple event that will run if the item is given to the player.
///     It will be run even if the item didn't fit in the backpack and is spawned on the floor.
/// </summary>
[ByRefEvent]
public record struct ObjectiveItemGivenEvent(EntityUid ItemUid);

/// <summary>
///     Simple event that will run if the item is given to the player.
///     It will be run even if the item didn't fit in the backpack and is spawned on the floor.
/// </summary>
[ByRefEvent]
public record struct BeforeObjectiveItemGivenEvent(EntityUid ItemUid, bool Retry);
