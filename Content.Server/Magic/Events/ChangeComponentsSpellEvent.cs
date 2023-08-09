using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Server.Magic.Events;

/// <summary>
///     Spell that uses the magic of ECS to add & remove components. Components are first removed, then added.
/// </summary>
public sealed class ChangeComponentsSpellEvent : EntityTargetActionEvent
{
    // TODO allow it to set component data-fields?
    // for now a Hackish way to do that is to remove & add, but that doesn't allow you to selectively set specific data fields.

    [DataField("toAdd")]
    [AlwaysPushInheritance]
    public EntityPrototype.ComponentRegistry ToAdd = new();

    [DataField("toRemove")]
    [AlwaysPushInheritance]
    public HashSet<string> ToRemove = new();
}
