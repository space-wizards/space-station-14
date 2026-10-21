using Content.Shared.Interaction;
using Robust.Shared.Prototypes;

namespace Content.Server.Actions;

/// <summary>
///     This component enables an entity to perform actions when used to interact with the world, without actually
///     granting that action to the entity that is using the item.
/// </summary>
/// <remarks>
///     If the entity is used in hand (<see cref="ActivateInWorldEvent"/>), it will perform a random available instant
///     action. If the entity is used to interact with another entity (<see cref="InteractUsingEvent"/>), it will
///     attempt to perform a random entity target action. Finally, if the entity is used to click somewhere in the world
///     and no other interaction takes place (<see cref="AfterInteractEvent"/>), then it will try to perform a random
///     available entity or world target action. This component does not bypass standard interaction checks.
///
///     This component mainly exists as a lazy way to add utility entities that can do things like cast "spells".
/// </remarks>
[RegisterComponent]
public sealed partial class ActionOnInteractComponent : Component
{
    [DataField(required: true)]
    public List<EntProtoId>? Actions;

    [DataField] public List<EntityUid>? ActionEntities;

    [DataField] public bool RequiresCharge;
}
