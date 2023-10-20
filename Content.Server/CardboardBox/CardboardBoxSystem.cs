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
using Content.Shared.Storage.Components;
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
        SubscribeLocalEvent<CardboardBoxComponent, StorageBeforeOpenEvent>(BeforeStorageOpen);
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterCloseEvent>(AfterStorageClosed);
		SubscribeLocalEvent<CardboardBoxComponent, ActivateInWorldEvent>(OnInteracted);
        SubscribeLocalEvent<CardboardBoxComponent, InteractedNoHandEvent>(OnNoHandInteracted);
        SubscribeLocalEvent<CardboardBoxComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<CardboardBoxComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<CardboardBoxComponent, DamageChangedEvent>(OnDamage);
    }

	private void OnInteracted(EntityUid uid, CardboardBoxComponent component, ActivateInWorldEvent args)
    {
		if (!TryComp<EntityStorageComponent>(uid, out var box))
            return;

        args.Handled = true;
        _storage.ToggleOpen(args.User, uid, box);

		if (box.Contents.Contains(args.User) && !box.Open)
		{
			_mover.SetRelay(args.User, uid);
			component.Mover = args.User;
		}
    }

    private void OnNoHandInteracted(EntityUid uid, CardboardBoxComponent component, InteractedNoHandEvent args)
    {
        //Free the mice please
        if (!TryComp<EntityStorageComponent>(uid, out var box) || box.Open || !box.Contents.Contains(args.User))
            return;

        _storage.OpenStorage(uid);
    }

    private void BeforeStorageOpen(EntityUid uid, CardboardBoxComponent component, ref StorageBeforeOpenEvent args)
    {
		if (component.Quiet)
			return;

        //Play effect & sound
        if (component.Mover != null)
        {
            if (_timing.CurTime > component.EffectCooldown)
            {
                RaiseNetworkEvent(new PlayBoxEffectMessage(GetNetEntity(uid), GetNetEntity(component.Mover.Value)));
                _audio.PlayPvs(component.EffectSound, uid);
                component.EffectCooldown = _timing.CurTime + component.CooldownDuration;
            }
        }
    }

    private void AfterStorageOpen(EntityUid uid, CardboardBoxComponent component, ref StorageAfterOpenEvent args)
    {
        // If this box has a stealth/chameleon effect, disable the stealth effect while the box is open.
        _stealth.SetEnabled(uid, false);
    }

    private void AfterStorageClosed(EntityUid uid, CardboardBoxComponent component, ref StorageAfterCloseEvent args)
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

        if (component.Mover == null)
        {
            _mover.SetRelay(args.Entity, uid);
			component.Mover = args.Entity;
        }
    }

    /// <summary>
    /// Through e.g. teleporting, it's possible for the mover to exit the box without opening it.
    /// Handle those situations but don't play the sound.
    /// </summary>
    private void OnEntRemoved(EntityUid uid, CardboardBoxComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Entity != component.Mover)
            return;

        RemComp<RelayInputMoverComponent>(component.Mover.Value);
        component.Mover = null;
    }
}
