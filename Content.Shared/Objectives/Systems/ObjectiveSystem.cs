using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives.Systems;

public sealed class ObjectiveSystem : EntitySystem
{
    /// <summary>
    /// Creates a new Objective instance for storing on the mind.
    /// </summary>
    /// <remarks>
    /// In the future this could be changed to an entity, with an objective component.
    /// </remarks>
    public Objective CreateObjective(EntityUid mindId, MindComponent mind, ObjectivePrototype proto)
    {
        // spawn all the condition entities from the prototype
        var conditions = new List<EntityUid>(proto.Conditions.Count);
        foreach (var id in proto.Conditions)
        {
            var cond = Spawn(id);
            if (!HasComp<ObjectiveConditionComponent>(cond))
            {
                Del(cond);
                Log.Error($"Invalid objective condition prototype {id} from {proto.ID}");
                continue;
            }

            var ev = new ConditionAssignedEvent(mindId, mind);
            RaiseLocalEvent(cond, ref ev);
            if (ev.Cancelled)
            {
                Del(cond);
                Log.Warning($"Could not assign objective condition {id} from {proto.ID}");
                continue;
            }

            conditions.Add(cond);
        }

        return new Objective(mindId, proto, conditions);
    }

    /// <summary>
    /// Get the title, description and progress of an objective condition using <see cref="ConditionGetInfoEvent"/>.
    /// </summary>
    /// <param name="uid"/>ID of the condition entity</param>
    /// <param name="mindId"/>ID of the player's mind entity</param>
    /// <param name="mind"/>Mind component of the player's mind</param>
    public ConditionInfo GetConditionInfo(EntityUid uid, EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return new ConditionInfo(null, null, null, null);

        var ev = new ConditionGetInfoEvent(mindId, mind, new ConditionInfo(null, null, null, null));
        RaiseLocalEvent(uid, ref ev);

        var info = ev.Info;
        if (info.Title == null || info.Description == null || info.Icon == null || info.Progress == null)
        {
            Log.Error($"An objective {objective.Prototype.Id} of {ToPrettyString(entity):player} has incomplete info: {condition.Title} {condition.Description} {condition.Progress}");
            info.Title ??= "!!!BROKEN OBJECTIVE!!!";
            info.Description ??= "!!! BROKEN OBJECTIVE DESCRIPTION!!!";
            info.Icon ??= new SpriteSpecifier.Rsi(new ("error.rsi"), "error.rsi");
            info.Progress ??= 0f;
        }

        return info;
    }

    /// <summary>
    /// Helper for mind to get a condition's title easily
    /// </summary>
    public string GetConditionTitle(EntityUid uid, EntityUid mindId, MindComponent? mind = null)
    {
        return GetConditionInfo(uid, mindId, mind).Title!;
    }
}
