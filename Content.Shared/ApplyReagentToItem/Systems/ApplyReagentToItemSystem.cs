using Content.Shared.Administration.Logs;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Content.Shared.ReagentOnItem;
using Content.Shared.Timing;

namespace Content.Shared.ApplyReagentToItem;

/// <summary>
///     This allows an item to apply reagents to items. For example,
///     a glue bottle has this because you need to be
///     able to apply the glue to items!
/// </summary>
public sealed class ApplyReagentToItemSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly OpenableSystem _openable = default!;
    [Dependency] private readonly ReagentOnItemSystem _reagentOnItem = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ApplyReagentToItemComponent, AfterInteractEvent>(OnInteract, after: new[] { typeof(OpenableSystem) });
        SubscribeLocalEvent<ApplyReagentToItemComponent, GetVerbsEvent<UtilityVerb>>(OnUtilityVerb);
    }

    private void OnInteract(Entity<ApplyReagentToItemComponent> entity, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!args.CanReach || args.Target is not { Valid: true } target || !HasComp<ItemComponent>(target))
            return;

        if (TryToApplyReagent(entity, target, args.User))
            args.Handled = true;
    }

    private void OnUtilityVerb(Entity<ApplyReagentToItemComponent> entity, ref GetVerbsEvent<UtilityVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess || args.Target is not { Valid: true } target
            || _openable.IsClosed(entity) || !HasComp<ItemComponent>(target))
            return;

        var user = args.User;

        var verb = new UtilityVerb()
        {
            Act = () => TryToApplyReagent(entity, target, user),
            IconEntity = GetNetEntity(entity),
            Text = Loc.GetString("apply-reagent-verb-text"),
            Message = Loc.GetString("apply-reagent-verb-message")
        };

        args.Verbs.Add(verb);
    }

    /// <summary>
    ///     Try to apply the reagent thats stored inside the squeeze bottle into an object.
    ///     If there are multiple reagents, it will try to apply all of them.
    /// </summary>
    private bool TryToApplyReagent(Entity<ApplyReagentToItemComponent> entity, EntityUid target, EntityUid actor)
    {

        if (!TryComp(entity, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((entity, useDelay)))
            return false;

        if (!HasComp<ItemComponent>(target))
        {
            return false;
        }

        _useDelay.TryResetDelay((entity, useDelay));

        if (HasComp<ItemComponent>(target)
            && _solutionContainer.TryGetSolution(entity.Owner, entity.Comp.Solution, out var solComp, out var solution))
        {
            var reagent = _solutionContainer.SplitSolution(solComp.Value, entity.Comp.AmountConsumedOnUse);

            if (_reagentOnItem.ApplyReagentEffectToItem(target, reagent))
            {
                _adminLogger.Add(LogType.Action, LogImpact.Medium, $"{ToPrettyString(actor):actor} tried to apply reagent to {ToPrettyString(target):subject} with {ToPrettyString(entity.Owner):tool}");
                _audio.PlayPredicted(entity.Comp.OnSqueezeNoise, entity, actor);
            }
            else
            {
                _popup.PopupPredicted(Loc.GetString("apply-reagent-is-empty-failure"), actor, actor, PopupType.Medium);
            }

            return true;
        }

        return false;
    }
}
