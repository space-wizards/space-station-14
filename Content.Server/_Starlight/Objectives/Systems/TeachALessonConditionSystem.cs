using Content.Server.Objectives.Components;
using Content.Server._Starlight.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Objectives.Components;

namespace Content.Server._Starlight.Objectives.Systems;

/// <summary>
/// Handles Teach a Lesson logic on if a specific entity has died at least once during the round
/// </summary>
public sealed class TeachALessonConditionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<TeachALessonTargetComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<TeachALessonConditionComponent, ObjectiveAfterAssignEvent>(OnAfterAssign);
        SubscribeLocalEvent<TeachALessonConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
    }

    private void OnGetProgress(Entity<TeachALessonConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.HasDied ? 1.0f : 0.0f;
    }

    private void OnAfterAssign(Entity<TeachALessonConditionComponent> ent, ref ObjectiveAfterAssignEvent args)
    {
        if (!TryComp(ent.Owner, out TargetObjectiveComponent? targetObjective))
            return;
        var targetMindUid = targetObjective.Target;
        if (targetMindUid is null)
            return;
        if (!TryComp(targetMindUid, out MindComponent? targetMind))
            return;
        var targetMobUid = targetMind.CurrentEntity;
        if (targetMobUid is null)
            return;
        var targetComponent = EnsureComp<TeachALessonTargetComponent>(targetMobUid.Value);
        targetComponent.Teachers.Add(ent);
        
    }

    private void OnMobStateChanged(Entity<TeachALessonTargetComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead) 
            return;
        foreach (var teacher in ent.Comp.Teachers)
        {
            if(!TryComp(teacher, out TeachALessonConditionComponent? condition))
                continue;
            condition.HasDied = true;
            
        }
    }
}