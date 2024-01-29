using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Required component for an objective entity prototype.
/// </summary>
[RegisterComponent, Access(typeof(SharedObjectivesSystem))]
public sealed partial class ObjectiveComponent : Component
{
    /// <summary>
    /// Difficulty rating used to avoid assigning too many difficult objectives.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Difficulty;

    /// <summary>
    /// Organisation that issued this objective, used for grouping and as a header above common objectives.
    /// </summary>
    [DataField(required: true), ViewVariables(VVAccess.ReadWrite)]
    public string Issuer = string.Empty;

    /// <summary>
    /// Unique objectives can only have 1 per prototype id.
    /// Set this to false if you want multiple objectives of the same prototype.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Unique = true;

    /// <summary>
    /// Icon of this objective to display in the character menu.
    /// Can be specified by an <see cref="ObjectiveGetInfoEvent"/> handler but is usually done in the prototype.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
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
/// Event raised on an objective after everything has handled <see cref="ObjectiveAssignedEvent"/>.
/// Use this to set the objective's title description or icon.
/// </summary>
[ByRefEvent]
public record struct ObjectiveAfterAssignEvent(EntityUid MindId, MindComponent Mind, ObjectiveComponent Objective, MetaDataComponent Meta);

/// <summary>
/// Event raised on an objective to update the Progress field.
/// To use this yourself call <see cref="SharedObjectivesSystem.GetInfo"/> with the mind.
/// </summary>
[ByRefEvent]
public record struct ObjectiveGetProgressEvent(EntityUid MindId, MindComponent Mind, float? Progress = null);
