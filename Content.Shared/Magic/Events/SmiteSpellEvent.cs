using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class SmiteSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    // TODO: Make part of gib method
    /// <summary>
    ///     Should this smite delete all parts/mechanisms gibbed except for the brain?
    /// </summary>
    [DataField("deleteNonBrainParts")]
    public bool DeleteNonBrainParts = true;

    // TODO: Move to magic component
    [DataField("speech")]
    public string? Speech { get; private set; }
}
