using Content.Server.AI.Components;
using Content.Server.Explosion.Components;
using Content.Server.Hands.Components;
using Content.Server.Popups;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Server.HotPotato;

/// <summary>
/// This handles...
/// </summary>
public sealed class HotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HotPotatoComponent, AfterInteractEvent>(OnInteractUsing);
        SubscribeLocalEvent<HotPotatoComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (HasComp<UnremoveableComponent>(component.Owner))
            _popupSystem.PopupEntity("YOU CAN'T LET GO!", uid, Filter.Pvs(uid), PopupType.MediumCaution);
    }

    private void OnUseInHand(EntityUid uid, HotPotatoComponent component, UseInHandEvent args)
    {
        if (!component.IsActivated)
        {
            EnsureComp<UnremoveableComponent>(component.Owner);
            component.IsActivated = true;
        }
    }

    private void OnInteractUsing(EntityUid uid, HotPotatoComponent component, AfterInteractEvent args)
    {
        if (!component.IsActivated)
            return;

        if (args.User == args.Target)
            return;

        if (HasComp<NPCComponent>(args.Target))
        {
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-give-fail-not-sapient", ("item", component.Owner)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
            return;
        }

        if (TryComp<MobStateComponent>(args.Target, out var mobStateComponent) && mobStateComponent.CurrentState == DamageState.Critical)
        {
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-give-fail-not-conscious", ("item", component.Owner)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
            return;
        }

        if (!TryComp<HandsComponent>(args.Target, out var targetHandsComponent))
            return;
        if (!TryComp<HandsComponent>(args.User, out var userHandsComponent))
            return;

        if (!_handsSystem.TryGetEmptyHand(targetHandsComponent.Owner, out var emptyHand))
        {
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-give-fail", ("item", component.Owner), ("target", args.Target)), args.User, Filter.Pvs(args.User), PopupType.MediumCaution);
            return;
        }

        RemComp<UnremoveableComponent>(component.Owner);

        _handsSystem.TryDrop(args.User, checkActionBlocker: false, handsComp:userHandsComponent);

        if (!_handsSystem.TryPickup(args.Target.Value, component.Owner, emptyHand, false, true, handsComp: targetHandsComponent))
        {
            _popupSystem.PopupEntity("fumble!!!", args.User, Filter.Pvs(args.User), PopupType.MediumCaution);
            return;
        }

        _popupSystem.PopupEntity(Loc.GetString("hot-potato-give", ("user", args.User), ("item", component.Owner), ("target", args.Target)), args.User, Filter.Pvs(args.Used), PopupType.LargeCaution);
        EnsureComp<UnremoveableComponent>(component.Owner);
    }
}
