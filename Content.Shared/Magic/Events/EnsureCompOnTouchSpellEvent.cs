using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class EnsureCompOnTouchSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public string? Speech { get; private set; }

    [DataField]
    [AlwaysPushInheritance]
    public ComponentRegistry Components { get; private set; } = new();
}
