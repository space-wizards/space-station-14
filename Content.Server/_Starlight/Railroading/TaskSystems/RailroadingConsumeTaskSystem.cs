using System.Linq;
using Content.Server._Starlight.Objectives.Events;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Nutrition;
using Content.Shared.Objectives;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingConsumeTaskSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IAdminManager _admins = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly RailroadingSystem _railroading = default!;
    [Dependency] private readonly StarlightEntitySystem _entitySystem = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RailroadConsumeTaskComponent, RailroadingCardChosenEvent>(OnConsumeTaskPicked);
        SubscribeLocalEvent<RailroadConsumeTaskComponent, RailroadingCardCompletionQueryEvent>(OnConsumeTaskCompletionQuery);
        SubscribeLocalEvent<RailroadConsumeTaskComponent, CollectObjectiveInfoEvent>(OnCollectObjectiveInfo);

        SubscribeLocalEvent<RailroadConsumeWatcherComponent, ConsumedFoodEvent>(OnFullyEaten);
    }

    private void OnFullyEaten(Entity<RailroadConsumeWatcherComponent> ent, ref ConsumedFoodEvent args)
    {
        if (!TryComp<RailroadableComponent>(ent, out var railroadable)
            || railroadable.ActiveCard is null
            || !TryComp<RailroadConsumeTaskComponent>(railroadable.ActiveCard, out var task))
            return;

        if (task.Objects.Contains(args.Food))
        {
            task.IsCompleted = true;
            RemComp<RailroadConsumeWatcherComponent>(ent);
            _railroading.InvalidateProgress((ent, railroadable));
        }
    }

    private void OnCollectObjectiveInfo(Entity<RailroadConsumeTaskComponent> ent, ref CollectObjectiveInfoEvent args)
    {
        if (!TryComp<RailroadCardComponent>(ent.Owner, out var card))
            return;

        var prototype = _proto.Index(ent.Comp.Objects.FirstOrDefault());
        args.Objectives.Add(new ObjectiveInfo
        {
            Title = Loc.GetString(ent.Comp.Message, ("Target", Loc.GetString(prototype.Name))),
            Icon = ent.Comp.Icon,
            Progress = ent.Comp.IsCompleted ? 1.0f : 0.0f,
        });
    }

    private void OnConsumeTaskCompletionQuery(Entity<RailroadConsumeTaskComponent> ent, ref RailroadingCardCompletionQueryEvent args)
    {
        if (args.IsCompleted == false) return;

        args.IsCompleted = ent.Comp.IsCompleted;
    }

    private void OnConsumeTaskPicked(Entity<RailroadConsumeTaskComponent> ent, ref RailroadingCardChosenEvent args) 
        => EnsureComp<RailroadConsumeWatcherComponent>(args.Subject.Owner);
}
