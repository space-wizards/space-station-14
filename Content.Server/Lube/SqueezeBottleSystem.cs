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

namespace Content.Server.Lube;

public sealed class SqueezeBottleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly PuddleSystem _puddle = default!;

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
        // SHOULD I BE ABLE TO DOUBLE UP ON APPLICATION OR ONLY ONE?
        // Split this into better cases.
        if (!HasComp<ItemComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
            return false;
        }
        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out _, out var solution))
        {
            Log.Log(LogLevel.Debug, "Here 1");
            var reagent = solution.SplitSolution(entity.Comp.AmountConsumedOnUse);
            if (reagent.Volume > 0)
            {
                Log.Log(LogLevel.Debug, "Here 2");

                if (!HasComp<NonStickSurfaceComponent>(target))
                {
                    var totalConsumed = FixedPoint2.New(0);
                    var amountOfSpaceLube = reagent.RemoveReagent("SpaceLube", entity.Comp.AmountConsumedOnUse - totalConsumed);
                    totalConsumed += amountOfSpaceLube;
                    var amountOfSpaceGlue = reagent.RemoveReagent("SpaceGlue", entity.Comp.AmountConsumedOnUse - totalConsumed);

                    if (amountOfSpaceLube > 0)
                    {
                        Log.Log(LogLevel.Debug, "Here 3");
                        var lubed = EnsureComp<SpaceLubeOnItemComponent>(target);
                        lubed.AmountOfReagentLeft += amountOfSpaceLube.Double();
                        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} lubed {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                        _audio.PlayPvs(entity.Comp.Squeeze, entity.Owner);
                        _popup.PopupEntity(Loc.GetString("lube-success", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);

                    }
                    if (amountOfSpaceGlue > 0)
                    {
                        Log.Log(LogLevel.Debug, "Here 4");
                        var glued = EnsureComp<SpaceGlueOnItemComponent>(target);
                        glued.AmountOfReagentLeft += amountOfSpaceLube.Double();
                        _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} lubed {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                        _audio.PlayPvs(entity.Comp.Squeeze, entity.Owner);
                        _popup.PopupEntity("item glued", actor, actor, PopupType.Medium);
                    }
                }

                _puddle.TrySpillAt(target, reagent, out var puddle, false);

                // Add the ReagetOnComponent with either lube or glue whichever is more.
                // Spill the rest of the liquid.

                // var amountOfSpaceLube = reagent.RemoveReagent("SpaceLube", entity.Comp.AmountConsumedOnUse);
                // var amountOfSpaceGlue = reagent.RemoveReagent("SpaceGlue", entity.Comp.AmountConsumedOnUse);
                // var reagentToAddToObject;

                // var lubed = EnsureComp<LubedComponent>(target);
                // lubed.SlipsLeft = _random.Next(entity.Comp.MinSlips * quantity.Int(), entity.Comp.MaxSlips * quantity.Int());
                // lubed.SlipStrength = entity.Comp.SlipStrength;
                // _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} lubed {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                // _audio.PlayPvs(entity.Comp.Squeeze, entity.Owner);
                // _popup.PopupEntity(Loc.GetString("lube-success", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);

                return true;
            }
        }
        _popup.PopupEntity(Loc.GetString("lube-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
        return false;
    }
}
