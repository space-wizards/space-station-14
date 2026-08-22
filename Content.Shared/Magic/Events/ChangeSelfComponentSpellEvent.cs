using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

/// <summary>
/// Spell that uses the magic of ECS to add & remove own components. Components are first removed, then added.
/// When reused, it removes the added components and returns the deleted ones.
/// </summary>
public sealed partial class ChangeOwnComponentsSpellEvent : InstantActionEvent
{

    /// <summary>
    /// The added components of the entity. If they are already there, they will not be added.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToAdd = new();

    /// <summary>
    /// The added components of the entity. If this component is already present, the new one will replace the old one.
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ForcedAdd = new();

    /// <summary>
    /// The removed components of the entity
    /// </summary>
    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToRemove = new();

    [DataField]
    public bool ComponentsAdded;

    [DataField]
    public ComponentRegistry AddedComponents = new();

    [DataField]
    public ComponentRegistry RemovedComponents = new();

}
