using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed class RuneMagicEvent : WorldTargetActionEvent
{
    [DataField("rune", required: true)]
    public string Rune = default!;
}
