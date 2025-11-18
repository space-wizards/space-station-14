using Content.Shared.Audio;
using Content.Shared.Damage.Systems;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Content.Shared.Trigger;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared.HotPotato;

public abstract class SharedHotPotatoSystem : EntitySystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAmbientSoundSystem _ambientSound = default!;
    [Dependency] private readonly DamageOnHoldingSystem _damageOnHolding = default!;
    [Dependency] private readonly IGameTiming _timing = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HotPotatoComponent, ContainerGettingRemovedAttemptEvent>(OnRemoveAttempt);
        SubscribeLocalEvent<HotPotatoComponent, ActiveTimerTriggerEvent>(OnActiveTimer);
        SubscribeLocalEvent<HotPotatoComponent, MeleeHitEvent>(OnMeleeHit);
    }

    private void OnRemoveAttempt(Entity<HotPotatoComponent> ent, ref ContainerGettingRemovedAttemptEvent args)
    {
        if (!_timing.ApplyingState && !ent.Comp.CanTransfer)
            args.Cancel();
    }

    private void OnActiveTimer(Entity<HotPotatoComponent> ent, ref ActiveTimerTriggerEvent args)
    {
        EnsureComp<ActiveHotPotatoComponent>(ent);
        ent.Comp.CanTransfer = false;
        _ambientSound.SetAmbience(ent.Owner, true);
        _damageOnHolding.SetEnabled(ent.Owner, true);
        Dirty(ent);
    }

    private void OnMeleeHit(Entity<HotPotatoComponent> ent, ref MeleeHitEvent args)
    {
        if (!HasComp<ActiveHotPotatoComponent>(ent))
            return;

        ent.Comp.CanTransfer = true;
        foreach (var hitEntity in args.HitEntities)
        {
            if (!TryComp<HandsComponent>(hitEntity, out var hands))
                continue;

            if (!_hands.IsHolding((hitEntity, hands), ent.Owner, out _) && _hands.TryForcePickupAnyHand(hitEntity, ent.Owner, handsComp: hands))
            {
                _popup.PopupPredicted(
                    Loc.GetString("hot-potato-passed", ("from", Identity.Entity(args.User, EntityManager)), ("to", Identity.Entity(hitEntity, EntityManager))),
                    ent.Owner,
                    args.User,
                    PopupType.Medium);
                break;
            }

            _popup.PopupClient(
                Loc.GetString("hot-potato-failed", ("to", Identity.Entity(hitEntity, EntityManager))),
                ent.Owner,
                args.User,
                PopupType.Medium);

            break;
        }

        ent.Comp.CanTransfer = false;
    }
}
