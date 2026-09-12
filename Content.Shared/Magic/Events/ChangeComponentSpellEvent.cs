using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

/// <summary>
/// Spell that uses the magic of ECS to add & remove components. Components are first removed, then added.
/// </summary>
public sealed partial class ChangeComponentsSpellEvent : EntityTargetActionEvent
{

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToAdd = new();

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToRemove = new();

}
