using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class KnockSpellEvent : InstantActionEvent
{
    /// <summary>
    /// The range this spell opens doors in
    /// 10f is the default
    /// Should be able to open all doors/lockers in visible sight
    /// </summary>
    [DataField]
    public float Range = 10f;
}
