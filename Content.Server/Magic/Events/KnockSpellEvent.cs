using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed class KnockSpellEvent : InstantActionEvent
{
    /// <summary>
    /// The range this spell opens doors in
    /// 4f is the default
    /// </summary>
    [DataField("range")]
    public float Range = 4f;
}
