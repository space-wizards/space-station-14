using Content.Shared.Actions;

namespace Content.Shared.Magic.Events;

public sealed partial class SmiteSpellEvent : EntityTargetActionEvent
{
    // TODO: Make part of gib method
    /// <summary>
    /// Should this smite delete all parts/mechanisms gibbed except for the brain?
    /// </summary>
    [DataField]
    public bool DeleteNonBrainParts = true;
}
