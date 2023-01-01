using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.CardboardBox;
using Content.Shared.CardboardBox.Components;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Containers;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server.CardboardBox;

public sealed class CardboardBoxSystem : SharedCardboardBoxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityStorageSystem _storage = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterOpenEvent>(AfterStorageOpen);
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterCloseEvent>(AfterStorageClosed);
        SubscribeLocalEvent<CardboardBoxComponent, InteractedNoHandEvent>(OnNoHandInteracted);
        SubscribeLocalEvent<CardboardBoxComponent, EntInsertedIntoContainerMessage>(OnEntInserted);

        SubscribeLocalEvent<CardboardBoxComponent, DamageChangedEvent>(OnDamage);
    }

    private void OnNoHandInteracted(EntityUid uid, CardboardBoxComponent component, InteractedNoHandEvent args)
    {
        //Free the mice please
        if (!TryComp<EntityStorageComponent>(uid, out var box) || box.Open || !box.Contents.Contains(args.User))
            return;

        _storage.OpenStorage(uid);
    }

    private void AfterStorageOpen(EntityUid uid, CardboardBoxComponent component, StorageAfterOpenEvent args)
    {
        //Remove the mover after the box is opened and play the effect if it hasn't been played yet.
        if (component.Mover != null)
        {
            RemComp<RelayInputMoverComponent>(component.Mover.Value);
            if (_timing.CurTime > component.EffectCooldown)
            {
                RaiseNetworkEvent(new PlayBoxEffectMessage(component.Owner, component.Mover.Value), Filter.PvsExcept(component.Owner));
                _audio.PlayPvs(component.EffectSound, component.Owner);
                component.EffectCooldown = _timing.CurTime + CardboardBoxComponent.MaxEffectCooldown;
            }
        }

        component.Mover = null;

        // If this box has a stealth/chameleon effect, disable the stealth effect while the box is open.
        _stealth.SetEnabled(uid, false);
    }

    private void AfterStorageClosed(EntityUid uid, CardboardBoxComponent component, StorageAfterCloseEvent args)
    {
        // If this box has a stealth/chameleon effect, enable the stealth effect.
        if (TryComp(uid, out StealthComponent? stealth))
        {
            _stealth.SetVisibility(uid, stealth.MaxVisibility, stealth);
            _stealth.SetEnabled(uid, true, stealth);
        }
    }

    //Relay damage to the mover
    private void OnDamage(EntityUid uid, CardboardBoxComponent component, DamageChangedEvent args)
    {
        if (args.DamageDelta != null && args.DamageIncreased)
        {
            _damageable.TryChangeDamage(component.Mover, args.DamageDelta, origin: args.Origin);
        }
    }

    private void OnEntInserted(EntityUid uid, CardboardBoxComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp(args.Entity, out MobMoverComponent? mover))
            return;

        if (component.Mover != null)
        {
            // player movers take priority
            if (HasComp<ActorComponent>(component.Mover) || !HasComp<ActorComponent>(args.Entity))
                return;

            RemComp<RelayInputMoverComponent>(component.Mover.Value);
        }

        var relay = EnsureComp<RelayInputMoverComponent>(args.Entity);
        _mover.SetRelay(args.Entity, uid, relay);
        component.Mover = args.Entity;
    }
}
