using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed class RuneMagicEvent : InstantActionEvent
{
    [DataField("rune", required: true)]
    public string RunePrototype = default!;
}
