using System.Linq;
using System.Threading.Tasks;
using Content.Server._Starlight.Objectives.Events;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Abilities.Goliath;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition;
using Content.Shared.Objectives;
using Robust.Server.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingTimerTaskSystem : AccUpdateEntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RailroadingSystem _railroading = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadTimerTaskComponent, RailroadingCardChosenEvent>(OnTaskPicked);
        SubscribeLocalEvent<RailroadTimerTaskComponent, RailroadingCardCompletionQueryEvent>(OnTaskCompletionQuery);
        SubscribeLocalEvent<RailroadTimerTaskComponent, CollectObjectiveInfoEvent>(OnCollectObjectiveInfo);
    }

    protected override void AccUpdate()
    {
        var query = EntityQueryEnumerator<RailroadTimerTaskComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (comp.IsCompleted) continue;
            if (comp.EndTime <= _timing.CurTime
                && TryComp<RailroadCardPerformerComponent>(ent, out var performer)
                && performer.Performer is Entity<RailroadableComponent> railroadable)
            {
                comp.IsCompleted = true;
                _railroading.InvalidateProgress(railroadable);
            }
        }
    }


    private void OnCollectObjectiveInfo(Entity<RailroadTimerTaskComponent> ent, ref CollectObjectiveInfoEvent args)
        => args.Objectives.Add(new ObjectiveInfo
        {
            Title = Loc.GetString(ent.Comp.Message, ("duration", ent.Comp.Duration.TotalMinutes)),
            Icon = ent.Comp.Icon,
            Progress = Math.Clamp((float)((_timing.CurTime - ent.Comp.Started).TotalSeconds / (_timing.CurTime - ent.Comp.EndTime).TotalSeconds), 0.0f, 1.0f)
        });

    private void OnTaskCompletionQuery(Entity<RailroadTimerTaskComponent> ent, ref RailroadingCardCompletionQueryEvent args)
    {
        if (args.IsCompleted == false) return;

        args.IsCompleted = ent.Comp.IsCompleted;
    }
    private void OnTaskPicked(Entity<RailroadTimerTaskComponent> ent, ref RailroadingCardChosenEvent args)
    {
        ent.Comp.Started = _timing.CurTime;
        ent.Comp.EndTime = ent.Comp.Started + ent.Comp.Duration;
    }
}
