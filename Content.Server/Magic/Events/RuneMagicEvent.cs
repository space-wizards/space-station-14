using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed class RuneMagicEvent : InstantActionEvent
{
    /// <summary>
    /// What type of rune this should spawn.
    /// </summary>
    [DataField("rune", required: true)]
    public string RunePrototype = default!;

}
