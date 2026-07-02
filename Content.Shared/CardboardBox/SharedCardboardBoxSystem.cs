using System.Numerics;
using Content.Shared.Access.Components;
using Content.Shared.CardboardBox.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.Interaction;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Storage.Components;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared.CardboardBox;

public abstract partial class SharedCardboardBoxSystem : EntitySystem
{
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedEntityStorageSystem _storage = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterOpenEvent>(AfterStorageOpen);
        SubscribeLocalEvent<CardboardBoxComponent, StorageBeforeOpenEvent>(BeforeStorageOpen);
        SubscribeLocalEvent<CardboardBoxComponent, StorageAfterCloseEvent>(AfterStorageClosed);
        SubscribeLocalEvent<CardboardBoxComponent, GetAdditionalAccessEvent>(OnGetAdditionalAccess);
        SubscribeLocalEvent<CardboardBoxComponent, ActivateInWorldEvent>(OnInteracted);
        SubscribeLocalEvent<CardboardBoxComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
        SubscribeLocalEvent<CardboardBoxComponent, EntRemovedFromContainerMessage>(OnEntRemoved);

        SubscribeLocalEvent<CardboardBoxComponent, DamageDealtEvent>(OnDamage);
    }

    private void OnInteracted(Entity<CardboardBoxComponent> ent, ref ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<EntityStorageComponent>(ent, out var box))
            return;

        if (!args.Complex)
        {
            if (box.Open || !box.Contents.Contains(args.User))
                return;
        }

        args.Handled = true;
        _storage.ToggleOpen(args.User, ent, box);

        if (!box.Contents.Contains(args.User) || box.Open)
            return;

        _mover.SetRelay(args.User, ent);
        ent.Comp.Mover = args.User;
        Dirty(ent);
    }

    private void OnGetAdditionalAccess(Entity<CardboardBoxComponent> ent, ref GetAdditionalAccessEvent args)
    {
        if (ent.Comp.Mover == null)
            return;

        args.Entities.Add(ent.Comp.Mover.Value);
    }

    private void BeforeStorageOpen(Entity<CardboardBoxComponent> ent, ref StorageBeforeOpenEvent args)
    {
        if (ent.Comp.Quiet)
            return;

        //  Play effect & sound.
        if (ent.Comp.Mover == null)
            return;

        if (_timing.CurTime <= ent.Comp.EffectCooldown)
            return;

        if (_net.IsServer)
            RaiseNetworkEvent(new PlayBoxEffectMessage(GetNetEntity(ent), GetNetEntity(ent.Comp.Mover.Value)));

        _audio.PlayPredicted(ent.Comp.EffectSound, ent, ent.Comp.Mover.Value);
        ent.Comp.EffectCooldown = _timing.CurTime + ent.Comp.CooldownDuration;
        Dirty(ent);
    }

    private void AfterStorageOpen(Entity<CardboardBoxComponent> ent, ref StorageAfterOpenEvent args)
    {
        // If this box has a stealth/chameleon effect, disable the stealth effect while the box is open.
        if (!TryComp<StealthComponent>(ent, out var stealth))
            return;

        _stealth.SetEnabled(ent, false, stealth);
    }

    private void AfterStorageClosed(Entity<CardboardBoxComponent> ent, ref StorageAfterCloseEvent args)
    {
        // If this box has a stealth/chameleon effect, enable the stealth effect.
        if (!TryComp<StealthComponent>(ent, out var stealth))
            return;

        _stealth.SetVisibility(ent, stealth.MaxVisibility, stealth);
        _stealth.SetEnabled(ent, true, stealth);
    }

    // Relay damage to the mover.
    private void OnDamage(Entity<CardboardBoxComponent> ent, ref DamageDealtEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Mover is not { } mover)
            return;

        _damageable.ChangeDamage(mover, args.Damage, origin: args.Origin);
    }

    private void OnEntInserted(Entity<CardboardBoxComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (!HasComp<MobMoverComponent>(args.Entity))
            return;

        if (ent.Comp.Mover != null)
            return;

        _mover.SetRelay(args.Entity, ent);
        ent.Comp.Mover = args.Entity;
        Dirty(ent);
    }

    /// <summary>
    /// Through e.g. teleporting, it's possible for the mover to exit the box without opening it.
    /// Handle those situations but don't play the sound.
    /// </summary>
    private void OnEntRemoved(Entity<CardboardBoxComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Entity != ent.Comp.Mover)
            return;

        // Stops movement after exit.
        _physics.SetLinearVelocity(ent, Vector2.Zero);
        RemComp<RelayInputMoverComponent>(ent.Comp.Mover.Value);
        ent.Comp.Mover = null;
        Dirty(ent);
    }
}
