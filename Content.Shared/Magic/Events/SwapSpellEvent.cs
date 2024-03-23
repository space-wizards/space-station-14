using Content.Shared.Actions;
using Robust.Shared.Audio;

namespace Content.Shared.Magic.Events;

public sealed partial class SwapSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    /// <summary>
    /// The range this spell opens doors in
    /// 4f is the default
    /// </summary>

    [DataField("speech")]
    public string? Speech { get; private set; }
}
