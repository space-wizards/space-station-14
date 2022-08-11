using Content.Server.AI.Components;
using Content.Server.Body.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Content.Shared.MobState.Components;
using Content.Shared.Popups;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.HotPotato;

/// <summary>
/// This handles...
/// </summary>
public sealed class HotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HotPotatoComponent, AfterInteractEvent>(OnInteractUsing);
        SubscribeLocalEvent<HotPotatoComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<HotPotatoComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(EntityUid uid, HotPotatoComponent component, TriggerEvent args)
    {
        if (component.IsDud)
        {
            RemComp<UnremoveableComponent>(component.Owner);
            var newItem = Spawn(component.TurnInto, Transform((component.Owner)).MapPosition);

            _audioSystem.PlayPvs(component.DudSound, newItem);
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-dud", ("item", component.Owner), ("newitem", newItem)), newItem, Filter.Pvs(newItem), PopupType.Medium);

            QueueDel(component.Owner);
        }
    }

    private void OnRemoveAttempt(EntityUid uid, HotPotatoComponent component, ContainerGettingRemovedAttemptEvent args)
    {
        if (HasComp<UnremoveableComponent>(component.Owner))
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-drop-fail", ("item", component.Owner)), uid, Filter.Entities(args.Container.Owner), PopupType.MediumCaution);
    }

    private void OnUseInHand(EntityUid uid, HotPotatoComponent component, UseInHandEvent args)
    {
        if (!component.IsActivated)
        {
            EnsureComp<UnremoveableComponent>(component.Owner);
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-activate", ("user", args.User), ("item", component.Owner)), args.User, Filter.Pvs(args.User), PopupType.LargeCaution);
            component.IsActivated = true;
        }
    }

    private void OnInteractUsing(EntityUid uid, HotPotatoComponent component, AfterInteractEvent args)
    {
        if (!component.IsActivated)
            return;

        if (args.User == args.Target)
            return;

        var currentTime = _gameTiming.CurTime;
        if (currentTime < component.CooldownEnd)
            return;

        component.LastUseTime = currentTime;
        component.CooldownEnd = component.LastUseTime + TimeSpan.FromSeconds(component.CooldownTime);

        if (!HasComp<BodyComponent>(args.Target)) // Just don't bother with things that don't have a body
            return;

        if (HasComp<NPCComponent>(args.Target)) // don't give to an npc
        {
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-give-fail-not-sapient", ("item", component.Owner)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
            return;
        }

        if (!TryComp<MobStateComponent>(args.Target, out var mobStateComponent) || mobStateComponent.CurrentState == DamageState.Critical || mobStateComponent.CurrentState == DamageState.Dead)
        {
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-give-fail-not-conscious", ("item", component.Owner)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
            return;
        }

        if (!TryComp<HandsComponent>(args.Target, out var targetHandsComponent))
            return;
        if (!TryComp<HandsComponent>(args.User, out var userHandsComponent))
            return;

        if (!_handsSystem.TryGetEmptyHand(targetHandsComponent.Owner, out var emptyHand)) // Check if the person has an empty hand
        {
            _popupSystem.PopupEntity(Loc.GetString("hot-potato-give-fail", ("item", component.Owner), ("target", args.Target)), args.User, Filter.Entities(args.User), PopupType.MediumCaution);
            return;
        }

        RemComp<UnremoveableComponent>(component.Owner); //Let the potato be removed for the transfer

        _handsSystem.TryDrop(args.User, checkActionBlocker: false, handsComp:userHandsComponent);
        _handsSystem.TryPickup(args.Target.Value, component.Owner, emptyHand, false, true, handsComp: targetHandsComponent);

        _popupSystem.PopupEntity(Loc.GetString("hot-potato-give", ("user", args.User), ("item", component.Owner), ("target", args.Target)), args.User, Filter.Pvs(args.Used), PopupType.LargeCaution);
        EnsureComp<UnremoveableComponent>(component.Owner);
    }
}
