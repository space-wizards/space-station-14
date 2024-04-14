using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class DevolveSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    /// <summary>
    ///     Should this smite delete all parts/mechanisms gibbed except for the brain?
    /// </summary>

    [DataField("speech")]
    public string? Speech { get; private set; }
}
