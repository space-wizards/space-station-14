using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.Lube;

public sealed partial class LubeSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private OpenableSystem _openable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private LubedSystem _lubed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LubeComponent, AfterInteractEvent>(OnInteract, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<LubeComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnInteract(Entity<LubeComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryLube(entity, target, args.User))
            args.Handled = true;
    }

    private void OnUtilityVerb(Entity<LubeComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Target is not { Valid: true } target ||
        _openable.IsClosed(entity))
            return;

        var user = args.User;

        var verb = new UtilityVerb()
        {
            Act = () => TryLube(entity, target, user),
            IconEntity = GetNetEntity(entity),
            Text = Loc.GetString("lube-verb-text"),
            Message = Loc.GetString("lube-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private bool TryLube(Entity<LubeComponent> entity, EntityUid target, EntityUid actor)
    {
        if (HasComp<LubedComponent>(target) || !HasComp<ItemComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solutionEntity, out _))
        {
            var quantity = _solutionContainer.RemoveReagent(solutionEntity.Value, entity.Comp.Reagent, entity.Comp.Consumption);
            if (quantity > 0)
            {
                _audio.PlayPredicted(entity.Comp.Squeeze, entity.Owner, actor);
                _popup.PopupClient(Loc.GetString("lube-success", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} lubed {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                var lubed = EnsureComp<LubedComponent>(target);
                var rand = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(entity));
                lubed.SlipsLeft = rand.Next(entity.Comp.MinSlips * quantity.Int(), 1 + entity.Comp.MaxSlips * quantity.Int());
                lubed.SlipStrength = entity.Comp.SlipStrength;
                if (_container.TryGetContainingContainer((target, null, null), out var container) && _hands.IsHolding(container.Owner, target))
                    _lubed.PerformLubedEffect((target, lubed), actor, out _);

                Dirty(target, lubed);
                return true;
            }
        }
        _popup.PopupClient(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
        return false;
    }
}
