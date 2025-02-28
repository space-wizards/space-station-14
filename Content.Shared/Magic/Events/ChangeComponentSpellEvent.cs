using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

/// <summary>
///     Spell that uses the magic of ECS to add & remove components. Components are first removed, then added.
/// </summary>
public sealed partial class ChangeComponentsSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    // TODO allow it to set component data-fields?
    // for now a Hackish way to do that is to remove & add, but that doesn't allow you to selectively set specific data fields.

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry ToAdd = new();

    [DataField]
    [AlwaysPushInheritance]
    public HashSet<string> ToRemove = new();

    [DataField]
    public string? Speech { get; private set; }

    [DataField]
    public bool DoSpeech { get; private set; }
}
