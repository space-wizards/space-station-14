using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Database;
using Content.Shared.Glue;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Lube;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Server.Lube;

public sealed class LubeSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;

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
            _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
        {
            var quantity = solution.RemoveReagent(entity.Comp.Reagent, entity.Comp.Consumption);
            if (quantity > 0)
            {
                var lubed = EnsureComp<LubedComponent>(target);
                lubed.SlipsLeft = _random.Next(entity.Comp.MinSlips * quantity.Int(), entity.Comp.MaxSlips * quantity.Int());
                lubed.SlipStrength = entity.Comp.SlipStrength;
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} lubed {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                _audio.PlayPvs(entity.Comp.Squeeze, entity.Owner);
                _popup.PopupEntity(Loc.GetString("lube-success", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
                return true;
            }
        }
        _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
        return false;
    }
}
