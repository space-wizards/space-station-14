using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Required component for an objective entity prototype.
/// Mostly it provides optional static data for the objective info event.
/// Progress cannot be provided this way, another system has to do that.
/// </summary>
[RegisterComponent, Access(typeof(SharedObjectivesSystem))]
public sealed partial class ObjectiveComponent : Component
{
    /// <summary>
    /// Difficulty rating used to avoid assigning too many difficult objectives.
    /// </summary>
    [DataField("difficulty", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Difficulty;

    /// <summary>
    /// Organisation that issued this objective, used for grouping and as a header above common objectives.
    /// </summary>
    [DataField("issuer", required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Issuer = string.Empty;

    /// <summary>
    /// Unique objectives can only have 1 per prototype id.
    /// Set this to false if you want multiple objectives of the same prototype.
    /// </summary>
    [DataField("unique"), ViewVariables(VVAccess.ReadWrite)]
    public bool Unique = true;

    /// <summary>
    /// Optional locale id to handle <see cref="ObjectiveGetInfoEvent"/>'s title.
    /// If another component provides it do not use this.
    /// </summary>
    [DataField("title"), ViewVariables(VVAccess.ReadWrite)]
    public string? Title;

    /// <summary>
    /// Optional locale id to handle <see cref="ObjectiveGetInfoEvent"/>'s description.
    /// If another component provides it do not use this.
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string? Description;

    /// <summary>
    /// Optional icon to handle <see cref="ObjectiveGetInfoEvent"/>'s icon.
    /// If another component provides it (should only be done if it depends on per-objective data) do not use this.
    /// </summary>
    [DataField("icon"), ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier? Icon;
}

/// <summary>
/// Event raised on an objective after spawning it to see if it meets all the requirements.
/// Requirement components should have subscriptions and cancel if the requirements are not met.
/// If a requirement is not met then the objective is deleted.
/// </summary>
[ByRefEvent]
public record struct RequirementCheckEvent(EntityUid MindId, MindComponent Mind, bool Cancelled = false);

/// <summary>
/// Event raised on an objective after its requirements have been checked.
/// If <see cref="Cancelled"/> is set to true, the objective is deleted.
/// Use this if the objective cannot be used, like a kill objective with no people alive.
/// </summary>
[ByRefEvent]
public record struct ObjectiveAssignedEvent(EntityUid MindId, MindComponent Mind, bool Cancelled = false);

/// <summary>
/// Event raised on an objective to get info about it.
/// In handlers set fields of <see cref="Info"/>.
/// To use this yourself call <see cref="SharedObjectivesSystem.GetObjectiveInfo"/> with the mind.
/// </summary>
[ByRefEvent]
public class ObjectiveGetInfoEvent
{
    public EntityUid MindId;
    public MindComponent Mind;
    public ObjectiveInfo Info;

    public ObjectiveGetInfoEvent(EntityUid mindId, MindComponent mind, ObjectiveInfo info)
    {
        MindId = mindId;
        Mind = mind;
        Info = info;
    }
}
