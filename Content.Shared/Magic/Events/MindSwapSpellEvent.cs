using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class MindSwapSpellEvent : EntityTargetActionEvent, ISpeakSpell
{


    [DataField]
    public string? Speech { get; private set; }
}
