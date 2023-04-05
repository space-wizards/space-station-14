using Content.Shared.Actions.ActionTypes;
using Content.Shared.Interaction;

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
public sealed class ActionOnInteractComponent : Component
{
    [DataField("activateActions")]
    public List<InstantAction>? ActivateActions;

    [DataField("entityActions")]
    public List<EntityTargetAction>? EntityActions;

    [DataField("worldActions")]
    public List<WorldTargetAction>? WorldActions;
}
