using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Changeling.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Changeling.Systems;

public sealed partial class ChangelingTransformationStingSystem : EntitySystem
{
    [Dependency] private HumanoidTransformStatusEffectSystem _humanoidTransformEffect = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    private readonly Dictionary<EntityUid, (EntityUid Target, EntityUid Action)> _pendingStingTargets = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingTransformationStingAbilityComponent, ChangelingTransformationStingEvent>(OnTransformationSting);
        SubscribeLocalEvent<ChangelingTransformComponent, ChangelingStingTransformIdentitySelectMessage>(OnIdentitySelected);
    }

    private void OnTransformationSting(Entity<ChangelingTransformationStingAbilityComponent> ent, ref ChangelingTransformationStingEvent args)
    {
        if (!TryComp<ChangelingIdentityComponent>(args.Performer, out var identityComp))
            return;

        if (identityComp.ConsumedIdentities.All(data => data.Identity == null))
        {
            _popup.PopupEntity(Loc.GetString("changeling-sting-transform-no-identities"), args.Performer, args.Performer);
            return;
        }

        _pendingStingTargets[args.Performer] = (args.Target, args.Action);

        if (!_ui.IsUiOpen(args.Performer, ChangelingTransformUiKey.StingKey, args.Performer))
            _ui.OpenUi(args.Performer, ChangelingTransformUiKey.StingKey, args.Performer);
    }

    private void OnIdentitySelected(Entity<ChangelingTransformComponent> ent, ref ChangelingStingTransformIdentitySelectMessage args)
    {
        if (!_pendingStingTargets.Remove(args.Actor, out var pending))
            return;

        var target = pending.Target;
        var action = pending.Action;

        // Revalidate action interaction constraints (range/access/etc.) at selection time.
        if (!TryComp<EntityTargetActionComponent>(action, out var targetAction) ||
            !_actions.ValidateEntityTarget(args.Actor, target, (action, targetAction)))
        {
            CloseStingUi(args.Actor);
            return;
        }

        if (!TryGetEntity(args.TargetIdentity, out var targetIdentity))
            return;

        if (!TryComp<ChangelingTransformationStingAbilityComponent>(action, out var stingComp))
            return;

        if (!_statusEffects.TryUpdateStatusEffectDuration(target,
            stingComp.TransformationStatusEffect,
            out var effectEnt,
            stingComp.TransformationDuration))
        {
            return;
        }

        _popup.PopupEntity(Loc.GetString("changeling-sting-success", ("target", Identity.Entity(target, EntityManager))), args.Actor, args.Actor);
        _popup.PopupEntity(Loc.GetString("changeling-sting-transform-target-popup"), target, target, PopupType.MediumCaution);

        _humanoidTransformEffect.RefreshHumanoidTransform(target, effectEnt.Value, targetIdentity.Value);
        _actions.StartUseDelay(action);
    }

    private void CloseStingUi(EntityUid user)
    {
        _ui.CloseUi(user, ChangelingTransformUiKey.StingKey, user);
    }
}
