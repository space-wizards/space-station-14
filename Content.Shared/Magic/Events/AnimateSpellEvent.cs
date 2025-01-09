using Content.Shared.Actions;
using Content.Shared.Whitelist;

namespace Content.Shared.Magic.Events;

public sealed partial class AnimateSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    [DataField]
    public string? Speech { get; private set; }
}
