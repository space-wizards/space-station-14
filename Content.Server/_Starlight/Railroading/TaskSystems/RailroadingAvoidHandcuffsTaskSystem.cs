using Content.Server._Starlight.Objectives.Events;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Cuffs;
using Content.Shared.Mobs;
using Content.Shared.Objectives;

namespace Content.Server._Starlight.Railroading;

// todo make TaskSystem<TTask,TWatcher>
public sealed partial class RailroadingAvoidHandcuffsTaskSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadAvoidHandcuffsTaskComponent, RailroadingCardChosenEvent>(OnTaskPicked);
        SubscribeLocalEvent<RailroadAvoidHandcuffsTaskComponent, RailroadingCardCompletionQueryEvent>(OnTaskCompletionQuery);
        SubscribeLocalEvent<RailroadAvoidHandcuffsTaskComponent, CollectObjectiveInfoEvent>(OnCollectObjectiveInfo);
        SubscribeLocalEvent<RailroadAvoidHandcuffsTaskComponent, RailroadingCardCompletedEvent>(OnCompleted);
        
        SubscribeLocalEvent<RailroadAvoidHandcuffsWatcherComponent, TargetHandcuffedEvent>(OnCuffedChanged);
    }

    private void OnCompleted(Entity<RailroadAvoidHandcuffsTaskComponent> ent, ref RailroadingCardCompletedEvent args)
        => RemComp<RailroadAvoidHandcuffsWatcherComponent>(args.Subject.Owner);

    private void OnCuffedChanged(Entity<RailroadAvoidHandcuffsWatcherComponent> ent, ref TargetHandcuffedEvent args)
    {
        if (!TryComp<RailroadableComponent>(ent, out var railroadable)
            || railroadable.ActiveCard is null
            || !TryComp<RailroadAvoidHandcuffsTaskComponent>(railroadable.ActiveCard, out var task))
            return;

        task.IsCompleted = false;
        RemComp<RailroadAvoidHandcuffsWatcherComponent>(ent);
    }

    private void OnCollectObjectiveInfo(Entity<RailroadAvoidHandcuffsTaskComponent> ent, ref CollectObjectiveInfoEvent args) 
        => args.Objectives.Add(new ObjectiveInfo
    {
        Title = Loc.GetString(ent.Comp.Message),
        Icon = ent.Comp.Icon,
        Progress = ent.Comp.IsCompleted ? 1.0f : 0.0f,
    });

    private void OnTaskCompletionQuery(Entity<RailroadAvoidHandcuffsTaskComponent> ent, ref RailroadingCardCompletionQueryEvent args)
    {
        if (args.IsCompleted == false) return;

        args.IsCompleted = ent.Comp.IsCompleted;
    }

    private void OnTaskPicked(Entity<RailroadAvoidHandcuffsTaskComponent> ent, ref RailroadingCardChosenEvent args) 
        => EnsureComp<RailroadAvoidHandcuffsWatcherComponent>(args.Subject.Owner);
}
