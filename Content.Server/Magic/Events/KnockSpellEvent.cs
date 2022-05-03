using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed class KnockSpellEvent : InstantActionEvent
{
    /// <summary>
    /// The range this spell opens doors in
    /// From what I understand 16 is the default view range.
    /// </summary>
    [DataField("range")]
    public float Range = 16f;
}
