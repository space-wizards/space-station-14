using Content.Shared.Actions;

namespace Content.Server.Magic.Events;

public sealed class SmiteSpellEvent : EntityTargetActionEvent, ISpeakSpell
{
    /// <summary>
    ///     Should this smite delete all parts/mechanisms gibbed except for the brain?
    /// </summary>
    [DataField("deleteNonBrainParts")]
    public bool DeleteNonBrainParts = true;

    [DataField("speech")]
    public string? Speech { get; }
}
