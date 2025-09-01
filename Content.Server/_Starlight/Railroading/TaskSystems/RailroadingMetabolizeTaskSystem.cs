using System.Linq;
using System.Threading.Tasks;
using Content.Server._Starlight.Objectives.Events;
using Content.Server.Administration.Managers;
using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Shared._Starlight.Railroading;
using Content.Shared._Starlight.Railroading.Events;
using Content.Shared.Administration.Logs;
using Content.Shared.Alert;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Nutrition;
using Content.Shared.Objectives;
using Robust.Server.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Starlight.Railroading;

public sealed partial class RailroadingMetabolizeTaskSystem : EntitySystem
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
        SubscribeLocalEvent<RailroadMetabolizeTaskComponent, RailroadingCardChosenEvent>(OnConsumeTaskPicked);
        SubscribeLocalEvent<RailroadMetabolizeTaskComponent, RailroadingCardCompletionQueryEvent>(OnConsumeTaskCompletionQuery);
        SubscribeLocalEvent<RailroadMetabolizeTaskComponent, CollectObjectiveInfoEvent>(OnCollectObjectiveInfo);

        SubscribeLocalEvent<RailroadMetabolizerWatcherComponent, RailroadingReagentMetabolizedEvent>(OnMetabolized);
    }

    private void OnMetabolized(Entity<RailroadMetabolizerWatcherComponent> ent, ref RailroadingReagentMetabolizedEvent args)
    {
        if (!TryComp<RailroadableComponent>(ent, out var railroadable)
            || railroadable.ActiveCard is null
            || !TryComp<RailroadMetabolizeTaskComponent>(railroadable.ActiveCard, out var task))
            return;

        var reagent = args.Reagent.Reagent;
        if (!task.Reagents.Any(x => x.Reagent == reagent))
            return;

        if (task.MetabolizedReagents.TryGetValue(reagent.Prototype, out var current))
            task.MetabolizedReagents[reagent.Prototype] = current + args.Reagent.Quantity;
        else
            task.MetabolizedReagents[reagent.Prototype] = args.Reagent.Quantity;

        if (task.Reagents.All(x => task.MetabolizedReagents.TryGetValue(x.Reagent.Prototype, out var quantity) && quantity >= x.Quantity))
        {
            RemComp<RailroadMetabolizerWatcherComponent>(ent);
            _railroading.InvalidateProgress((ent, railroadable));
        }
    }

    private void OnCollectObjectiveInfo(Entity<RailroadMetabolizeTaskComponent> ent, ref CollectObjectiveInfoEvent args)
    {
        foreach (var quantity in ent.Comp.Reagents)
        {
            var reagentProto = _proto.Index<ReagentPrototype>(quantity.Reagent.Prototype);
            args.Objectives.Add(new ObjectiveInfo
            {
                Title = Loc.GetString(ent.Comp.Message, ("Target", Loc.GetString(reagentProto.LocalizedName))),
                Icon = ent.Comp.Icon,
                Progress = ent.Comp.MetabolizedReagents.TryGetValue(quantity.Reagent.Prototype, out var metabolizedQuantity)
                    && quantity.Quantity > 0
                    ? Math.Clamp((metabolizedQuantity / quantity.Quantity).Float(), 0f, 1f) : 0f,
            });
        }
    }

    private void OnConsumeTaskCompletionQuery(Entity<RailroadMetabolizeTaskComponent> ent, ref RailroadingCardCompletionQueryEvent args)
    {
        if (args.IsCompleted == false) return;

        args.IsCompleted = ent.Comp.Reagents.All(x => ent.Comp.MetabolizedReagents.TryGetValue(x.Reagent.Prototype, out var quantity) && quantity >= x.Quantity);
    }

    private void OnConsumeTaskPicked(Entity<RailroadMetabolizeTaskComponent> ent, ref RailroadingCardChosenEvent args) 
        => EnsureComp<RailroadMetabolizerWatcherComponent>(args.Subject.Owner);
}
