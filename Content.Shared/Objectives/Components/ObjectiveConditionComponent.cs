using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Systems;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives.Components;

/// <summary>
/// Required component for an objective condition prototype.
/// Without it, the condition will not be added to the objective.
/// Functionally it provides optional static data for the condition info event.
/// Progress cannot be provided this way, another system has to do that.
/// </summary>
[RegisterComponent, Access(typeof(ObjectiveConditionSystem))]
public sealed partial class ObjectiveConditionComponent : Component
{
    /// <summary>
    /// Optional locale id to handle <see cref="ConditionGetInfoEvent"/>'s title.
    /// If another condition provides it do not use this.
    /// </summary>
    [DataField("title"), ViewVariables(VVAccess.ReadWrite)]
    public string? Title;

    /// <summary>
    /// Optional locale id to handle <see cref="ConditionGetInfoEvent"/>'s description.
    /// If another condition provides it do not use this.
    /// </summary>
    [DataField("description"), ViewVariables(VVAccess.ReadWrite)]
    public string? Description;

    /// <summary>
    /// Optional icon to handle <see cref="ConditionGetInfoEvent"/>'s icon.
    /// If another condition provides it (should only be done if it depends on per-component data) do not use this.
    /// </summary>
    [DataField("icon"), ViewVariables(VVAccess.ReadWrite)]
    public SpriteSpecifier? Icon;

    /// <summary>
    /// Difficulty rating used to avoid assigning too many difficult objectives.
    /// </summary>
    [DataField("difficulty", required: true), ViewVariables(VVAccess.ReadWrite)]
    public float Difficulty;
}

/// <summary>
/// Event raised on an objective condition after it has been created from the objective's prototype.
/// If <see cref="Cancelled"/> is set to true, the condition is deleted.
/// Use this if the objective cannot be used, like a kill objective with no people alive.
/// </summary>
[ByRefEvent]
public record struct ConditionAssignedEvent(EntityUid MindId, MindComponent Mind, bool Cancelled = false);

/// <summary>
/// Event raised on an objective condition to get info about it.
/// In handlers set fields of <see cref="Info"/>.
/// To use this yourself call <see cref="ObjectiveSystem.GetConditionInfo"/> with the mind.
/// </summary>
[ByRefEvent]
public class ConditionGetInfoEvent
{
    public EntityUid MindId;
    public MindComponent Mind;
    public ConditionInfo Info;

    public ConditionGetInfoEvent(EntityUid mindId, MindComponent mind, ConditionInfo info)
    {
        MindId = mindId;
        Mind = mind;
        Info = info;
    }
}
