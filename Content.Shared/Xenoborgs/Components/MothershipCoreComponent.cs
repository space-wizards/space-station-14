namespace Content.Shared.Xenoborgs.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

/// <summary>
/// Defines what is a xenoborg core for the intentions of the xenoborg rule. if all xenoborg cores are destroyed. all xenoborgs will self-destruct.
/// </summary>
[RegisterComponent]
public sealed partial class MothershipCoreComponent : Component
{
    /// <summary>
    /// The mindrole associated with the xenoborg core
    /// </summary>
    [DataField]
    public EntProtoId MindRole = "MindRoleMothershipCore";

    /// <summary>
    /// Briefing sound when you become a xenoborg core
    /// </summary>
    [DataField]
    public SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/Ambience/Antag/xenoborg_start.ogg");
}
