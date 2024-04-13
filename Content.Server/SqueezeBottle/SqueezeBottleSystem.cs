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
using Content.Shared.ReagentOnItem;
using Robust.Shared.Toolshed.Commands.Math;
using System.Diagnostics;
using Content.Shared.FixedPoint;
using Content.Server.Fluids.EntitySystems;
using Content.Shared.Clothing.Components;
using Content.Server.ReagentOnItem;

namespace Content.Server.SqueezeBottle;

public sealed class SqueezeBottleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;
    [Dependency] private readonly ReagentOnItemSystem _reagentOnItem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SqueezeBottleComponent, AfterInteractEvent>(OnInteract, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<SqueezeBottleComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnInteract(Entity<SqueezeBottleComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target)
            return;

        if (TryToApplyReagent(entity, target, args.User))
            args.Handled = true;
    }

    private void OnUtilityVerb(Entity<SqueezeBottleComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Target is not { Valid: true } target ||
        _openable.IsClosed(entity))
            return;

        var user = args.User;

        var verb = new UtilityVerb()
        {
            Act = () => TryToApplyReagent(entity, target, user),
            IconEntity = GetNetEntity(entity),
            Text = Loc.GetString("lube-verb-text"),
            Message = Loc.GetString("lube-verb-message")
        };

        args.Verbs.Add(verb);
    }

    private bool TryToApplyReagent(Entity<SqueezeBottleComponent> entity, EntityUid target, EntityUid actor)
    {
        // Split this into better cases.
        if (!HasComp<ItemComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solComp, out var solution))
        {
            var reagent = solution.SplitSolution(entity.Comp.AmountConsumedOnUse);
            _reagentOnItem.AddReagentToItem(target, reagent);

            return true;
        }
        _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
        return false;
    }
}
