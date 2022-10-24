using System.Linq;
using Content.Shared.CardboardBox.Components;
using Content.Server.Storage.Components;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.CardboardBox;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Content.Shared.Stealth.Components;
using Content.Shared.Stealth;
using Robust.Shared.Prototypes;

namespace Content.Server.CardboardBox;

public sealed class CardboardBoxSystem : SharedCardboardBoxSystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMoverController _mover = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CardboardBoxComponent, StorageBeforeCloseEvent>(OnBeforeStorageClosed);
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterOpenEvent>(AfterStorageOpen);
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterCloseEvent>(AfterStorageClosed);

        SubscribeLocalEvent<CardboardBoxComponent, DamageChangedEvent>(OnDamage);
        SubscribeLocalEvent<CardboardBoxComponent, DamageModifyEvent>(OnDamageModified);
    }

    private void OnBeforeStorageClosed(EntityUid uid, CardboardBoxComponent component, StorageBeforeCloseEvent args)
    {
        var mobMover = args.Contents.Where(HasComp<MobMoverComponent>).ToList();

        //Grab the first mob to set as the mover and to prevent other mobs from entering.
        foreach (var mover in mobMover)
        {
            //Set the movement relay for the box as the first mob
            if (component.Mover == null && args.Contents.Contains(mover))
            {
                var relay = EnsureComp<RelayInputMoverComponent>(mover);
                _mover.SetRelay(mover, uid, relay);
                component.Mover = mover;
            }

            if (mover != component.Mover)
                args.Contents.Remove(mover);
        }
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

    //Modify damage if applicable
    private void OnDamageModified(EntityUid uid, CardboardBoxComponent component, DamageModifyEvent args)
    {
        if (!TryComp<DamageableComponent>(uid, out var damageable) ||
            damageable.DamageModifierSetId == null ||
            !_proto.TryIndex(damageable.DamageModifierSetId, out DamageModifierSetPrototype? modifier))
            return;

        args.Damage = DamageSpecifier.ApplyModifierSet(args.Damage, modifier);
    }
}
