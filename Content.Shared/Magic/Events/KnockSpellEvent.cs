using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

public sealed partial class KnockSpellEvent : InstantActionEvent, ISpeakSpell
{
    /// <summary>
    /// The range this spell opens doors in
    /// 10f is the default
    ///   Should be able to open all doors/lockers in visible sight
    /// </summary>
    [DataField("range")]
    public float Range = 10f;

    [DataField("speech")]
    public string? Speech { get; private set; }
}
