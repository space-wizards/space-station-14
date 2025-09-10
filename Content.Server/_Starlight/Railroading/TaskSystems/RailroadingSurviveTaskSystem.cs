using Content.Server._Starlight.Objectives.Events;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Mobs;
using Content.Shared.Objectives;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingSurviveTaskSystem : EntitySystem
{
    [Dependency] private readonly RailroadingSystem _railroading = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadSurviveTaskComponent, RailroadingCardChosenEvent>(OnTaskPicked);
        SubscribeLocalEvent<RailroadSurviveTaskComponent, RailroadingCardCompletionQueryEvent>(OnTaskCompletionQuery);
        SubscribeLocalEvent<RailroadSurviveTaskComponent, CollectObjectiveInfoEvent>(OnCollectObjectiveInfo);
        SubscribeLocalEvent<RailroadSurviveTaskComponent, RailroadingCardCompletedEvent>(OnCompleted);
        
        SubscribeLocalEvent<RailroadSurviveWatcherComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RailroadSurviveWatcherComponent, RailroadingCardFailedEvent>(OnFailed);
    }

    private void OnFailed(Entity<RailroadSurviveWatcherComponent> ent, ref RailroadingCardFailedEvent args)
        => RemComp<RailroadAvoidHandcuffsWatcherComponent>(args.Subject.Owner);

    private void OnCompleted(Entity<RailroadSurviveTaskComponent> ent, ref RailroadingCardCompletedEvent args)
        => RemComp<RailroadSurviveWatcherComponent>(args.Subject.Owner);

    private void OnMobStateChanged(Entity<RailroadSurviveWatcherComponent> ent, ref MobStateChangedEvent args)
    {
        if (!TryComp<RailroadableComponent>(ent, out var railroadable)
            || railroadable.ActiveCard is null
            || !TryComp<RailroadSurviveTaskComponent>(railroadable.ActiveCard, out var task))
            return;

        if (args.NewMobState == MobState.Dead)
        {
            task.IsCompleted = false;
            _railroading.CardFailed((ent, railroadable));
        }
    }

    private void OnCollectObjectiveInfo(Entity<RailroadSurviveTaskComponent> ent, ref CollectObjectiveInfoEvent args) 
        => args.Objectives.Add(new ObjectiveInfo
    {
        Title = Loc.GetString(ent.Comp.Message),
        Icon = ent.Comp.Icon,
        Progress = ent.Comp.IsCompleted ? 1.0f : 0.0f,
    });

    private void OnTaskCompletionQuery(Entity<RailroadSurviveTaskComponent> ent, ref RailroadingCardCompletionQueryEvent args)
    {
        if (args.IsCompleted == false) return;

        args.IsCompleted = ent.Comp.IsCompleted;
    }

    private void OnTaskPicked(Entity<RailroadSurviveTaskComponent> ent, ref RailroadingCardChosenEvent args) 
        => EnsureComp<RailroadSurviveWatcherComponent>(args.Subject.Owner);
}
