using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared.Magic.Events;

public sealed partial class AnimateSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public string? Speech { get; private set; }

    [DataField]
    public ComponentRegistry AddComponents = new();

    [DataField]
    public HashSet<string> RemoveComponents = new();
}
