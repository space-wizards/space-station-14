using Content.Server._Starlight.Objectives.Events;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Delivery;
using Content.Shared.Objectives;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingDeliveryOpenTaskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly RailroadingSystem _railroading = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RailroadDeliveryOpenTaskComponent, RailroadingCardChosenEvent>(OnTaskPicked);
        SubscribeLocalEvent<RailroadDeliveryOpenTaskComponent, RailroadingCardCompletionQueryEvent>(OnCompletionQuery);
        SubscribeLocalEvent<RailroadDeliveryOpenTaskComponent, CollectObjectiveInfoEvent>(OnCollectObjectiveInfo);

        SubscribeLocalEvent<RailroadDeliveryOpenWatcherComponent, DeliveryOpenedEvent>(OnMailOpen);
    }

    private void OnMailOpen(Entity<RailroadDeliveryOpenWatcherComponent> ent, ref DeliveryOpenedEvent evt)
    {
        if (!TryComp<RailroadableComponent>(ent, out var railroadable)
            || railroadable.ActiveCard is null
            || !TryComp<RailroadDeliveryOpenTaskComponent>(railroadable.ActiveCard, out var task))
            return;

        if (++task.AmountOpened >= task.Amount)
        {
            RemComp<RailroadDeliveryOpenWatcherComponent>(ent);
            _railroading.InvalidateProgress((ent, railroadable));
        }
    }

    private void OnCollectObjectiveInfo(Entity<RailroadDeliveryOpenTaskComponent> ent, ref CollectObjectiveInfoEvent args) => args.Objectives.Add(new ObjectiveInfo
    {
        Title = Loc.GetString(ent.Comp.Message, ("Amount", ent.Comp.Amount)),
        Icon = ent.Comp.Icon,
        Progress = (float)ent.Comp.AmountOpened / ent.Comp.Amount,
    });

    private void OnCompletionQuery(Entity<RailroadDeliveryOpenTaskComponent> ent, ref RailroadingCardCompletionQueryEvent args)
    {
        if (args.IsCompleted == false) return;

        args.IsCompleted = ent.Comp.AmountOpened >= ent.Comp.Amount;
    }

    private void OnTaskPicked(Entity<RailroadDeliveryOpenTaskComponent> ent, ref RailroadingCardChosenEvent args) 
        => EnsureComp<RailroadDeliveryOpenWatcherComponent>(args.Subject.Owner);
}