using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class MindswapSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    /// <summary>
    ///     Should this smite delete all parts/mechanisms gibbed except for the brain?
    /// </summary>
    [DataField]
    public float stunTime = 5f;

    [DataField("speech")]
    public string? Speech { get; private set; }
}
