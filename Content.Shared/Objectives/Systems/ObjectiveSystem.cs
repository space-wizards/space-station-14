using Content.Shared.Mind;
using Content.Shared.Objectives;
using Content.Shared.Objectives.Components;
using Robust.Shared.Utility;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// Provides API for creating and interacting with objectives.
/// Adds default info from <see cref="ObjectiveComponent"/> to <see cref="ObjectiveGetInfoEvent"/>.
/// </summary>
public sealed class ObjectiveSystem : EntitySystem
{
    [Dependency] private readonly SharedMindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ObjectiveComponent, ObjectiveGetInfoEvent>(OnGetInfo);
    }

    private void OnGetInfo(EntityUid uid, ObjectiveComponent comp, ref ObjectiveGetInfoEvent args)
    {
        if (comp.Title != null)
            args.Info.Title = comp.Title;
        if (comp.Description != null)
            args.Info.Description = comp.Description;
        if (comp.Icon != null)
            args.Info.Icon = comp.Icon;
    }

    /// <summary>
    /// Checks requirements and duplicate objectives to see if an objective can be assigned.
    /// </summary>
    public bool CanBeAssigned(EntityUid uid, EntityUid mindId, MindComponent mind, ObjectiveComponent? comp = null)
    {
        if (!Resolve(uid, ref comp))
            return false;

        var ev = new RequirementCheckEvent(mindId, mind);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
            return false;

        // only check for duplicate prototypes if it's unique
        if (comp.Unique)
        {
            foreach (var objective in mind.AllObjectives)
            {
                if (objective.Prototype.ID == ID)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Spawns and assigns an objective for a mind.
    /// The objective is not added to the mind's objectives, mind system does that in TryAddObjective.
    /// If the objective could not be assigned the objective is deleted and null is returned.
    /// </summary>
    public EntityUid? TryCreateObjective(EntityUid mindId, MindComponent mind, string proto)
    {
        var uid = Spawn(proto);
        if (!TryComp<ObjectiveComponent>(uid, out var comp))
        {
            Del(uid);
            Log.Error($"Invalid objective prototype {proto}, missing ObjectiveComponent");
            return null;
        }

        Log.Debug($"Created objective {proto} ({uid}");

        if (!CanBeAssigned(uid, mindId, mind, comp))
        {
            Del(uid);
            Log.Warning($"Objective {uid} did not match the requirements for {_mind.MindOwnerLoggingString(mind)}, deleted it");
            return null;
        }

        var ev = new ObjectiveAssignedEvent(mindId, mind);
        RaiseLocalEvent(uid, ref ev);
        if (ev.Cancelled)
        {
            Del(uid);
            Log.Warning($"Could not assign objective {uid}, deleted it");
            return false;
        }

        return uid;
    }

    /// <summary>
    /// Get the title, description, icon and progress of an objective using <see cref="ObjectiveGetInfoEvent"/>.
    /// Any null fields are logged and replaced with fallbacks so they will never be null.
    /// </summary>
    /// <param name="uid"/>ID of the condition entity</param>
    /// <param name="mindId"/>ID of the player's mind entity</param>
    /// <param name="mind"/>Mind component of the player's mind</param>
    public ObjectiveInfo GetInfo(EntityUid uid, EntityUid mindId, MindComponent? mind = null)
    {
        if (!Resolve(mindId, ref mind))
            return new ObjectiveInfo(null, null, null, null);

        var ev = new ObjectiveGetInfoEvent(mindId, mind, new ObjectiveInfo(null, null, null, null));
        RaiseLocalEvent(uid, ref ev);

        var info = ev.Info;
        if (info.Title == null || info.Description == null || info.Icon == null || info.Progress == null)
        {
            Log.Error($"An objective {uid} of {_mind.MindOwnerLoggingString(mind)} has incomplete info: {info.Title} {info.Description} {info.Progress}");
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
    public string GetTitle(EntityUid uid, EntityUid mindId, MindComponent? mind = null)
    {
        return GetConditionInfo(uid, mindId, mind).Title!;
    }
}
