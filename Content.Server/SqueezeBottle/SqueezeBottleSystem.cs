using Content.Server.Administration.Logs;
using Content.Server.Chemistry.Containers.EntitySystems;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.SqueezeBottle;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;

using Content.Server.ReagentOnItem;

namespace Content.Server.SqueezeBottle;

/// <summary>
///     Squeeze bottles are to apply reagents to items. For example,
///     a glue bottle is a squeeze bottle because you need to be
///     able to apply the glue to items!
/// </summary>
public sealed class SqueezeBottleSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly IAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
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
            Text = Loc.GetString("squeeze-bottle-verb-text"),
            Message = Loc.GetString("squeeze-bottle-verb-message")
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Try to apply the reagent thats stored inside the squeeze bottle into an object.
    ///     If there are multiple reagents, it will try to apply all of them.
    /// </summary>
    private bool TryToApplyReagent(Entity<SqueezeBottleComponent> entity, EntityUid target, EntityUid actor)
    {
        // Squeeze bottles only work on items so if its not an item quit.
        if (!HasComp<ItemComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("squeeze-bottle-not-item-failure", ("target", Identity.Entity(target, EntityManager))), actor, actor, PopupType.Medium);
            return false;
        }

        if (HasComp<ItemComponent>(target) && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solComp, out var solution))
        {
            var reagent = _solutionContainer.SplitSolution(solComp.Value, entity.Comp.AmountConsumedOnUse);
            // If this fails, that means the squeeze bottle was empty.
            if (_reagentOnItem.AddReagentToItem(target, reagent))
            {
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} tried to apply reagent to {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                _audio.PlayPvs(entity.Comp.OnSqueezeNoise, entity.Owner);
            }
            else
            {
                _popup.PopupEntity(Loc.GetString("squeeze-bottle-is-empty-failure"), actor, actor, PopupType.Medium);
            }

            return true;
        }

        return false;
    }
}
