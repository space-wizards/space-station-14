using Content.Shared.DoAfter;
using Content.Shared.Emp;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;

    [MustCallBase]
    protected virtual void InitializeBallistic()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentInit>(OnBallisticInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, MapInitEvent>(OnBallisticMapInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, TakeAmmoEvent>(OnBallisticTakeAmmo);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetAmmoCountEvent>(OnBallisticAmmoCount);

        SubscribeLocalEvent<BallisticAmmoProviderComponent, ExaminedEvent>(OnBallisticExamine);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<Verb>>(OnBallisticVerb);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, InteractUsingEvent>(OnBallisticInteractUsing);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AfterInteractEvent>(OnBallisticAfterInteract);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AmmoFillDoAfterEvent>(OnBallisticAmmoFillDoAfter);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, UseInHandEvent>(OnBallisticUse);

        SubscribeLocalEvent<BallisticAmmoSelfRefillerComponent, MapInitEvent>(OnBallisticRefillerMapInit);
        SubscribeLocalEvent<BallisticAmmoSelfRefillerComponent, EmpPulseEvent>(OnRefillerEmpPulsed);
    }

    private void OnBallisticRefillerMapInit(Entity<BallisticAmmoSelfRefillerComponent> entity, ref MapInitEvent args)
    {
        entity.Comp.NextAutoRefill = Timing.CurTime + entity.Comp.AutoRefillRate;
    }

    private void OnBallisticUse(EntityUid uid, BallisticAmmoProviderComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User);
        args.Handled = true;
    }

    private void OnBallisticInteractUsing(EntityUid uid, BallisticAmmoProviderComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryBallisticInsert((uid, component), args.Used, args.User))
            args.Handled = true;
    }

    private void OnBallisticAfterInteract(EntityUid uid, BallisticAmmoProviderComponent component, AfterInteractEvent args)
    {
        if (args.Handled ||
            !component.MayTransfer ||
            !Timing.IsFirstTimePredicted ||
            args.Target == null ||
            args.Used == args.Target ||
            Deleted(args.Target) ||
            !TryComp<BallisticAmmoProviderComponent>(args.Target, out var targetComponent) ||
            targetComponent.Whitelist == null)
        {
            return;
        }

        args.Handled = true;

        // Continuous loading
        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, component.FillDelay, new AmmoFillDoAfterEvent(), used: uid, target: args.Target, eventTarget: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            NeedHand = true,
        });
    }

    private void OnBallisticAmmoFillDoAfter(EntityUid uid, BallisticAmmoProviderComponent component, AmmoFillDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (Deleted(args.Target) ||
            !TryComp<BallisticAmmoProviderComponent>(args.Target, out var target) ||
            target.Whitelist == null)
            return;

        if (target.Entities.Count + target.UnspawnedCount == target.Capacity)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-target-full",
                    ("entity", args.Target)),
                args.Target,
                args.User);
            return;
        }

        if (component.Entities.Count + component.UnspawnedCount == 0)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-empty",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }

        void SimulateInsertAmmo(EntityUid ammo, EntityUid ammoProvider, EntityCoordinates coordinates)
        {
            // We call SharedInteractionSystem to raise contact events. Checks are already done by this point.
            _interaction.InteractUsing(args.User, ammo, ammoProvider, coordinates, checkCanInteract: false, checkCanUse: false);
        }

        List<(EntityUid? Entity, IShootable Shootable)> ammo = new();
        var evTakeAmmo = new TakeAmmoEvent(1, ammo, Transform(uid).Coordinates, args.User);
        RaiseLocalEvent(uid, evTakeAmmo);

        foreach (var (ent, _) in ammo)
        {
            if (ent == null)
                continue;

            if (_whitelistSystem.IsWhitelistFail(target.Whitelist, ent.Value))
            {
                Popup(
                    Loc.GetString("gun-ballistic-transfer-invalid",
                        ("ammoEntity", ent.Value),
                        ("targetEntity", args.Target.Value)),
                    uid,
                    args.User);

                SimulateInsertAmmo(ent.Value, uid, Transform(uid).Coordinates);
            }
            else
            {
                // play sound to be cool
                Audio.PlayPredicted(component.SoundInsert, uid, args.User);
                SimulateInsertAmmo(ent.Value, args.Target.Value, Transform(args.Target.Value).Coordinates);
            }

            if (IsClientSide(ent.Value))
                Del(ent.Value);
        }

        // repeat if there is more space in the target and more ammo to fill
        var moreSpace = target.Entities.Count + target.UnspawnedCount < target.Capacity;
        var moreAmmo = component.Entities.Count + component.UnspawnedCount > 0;
        args.Repeat = moreSpace && moreAmmo;
    }

    private void OnBallisticVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null || !component.Cycleable)
            return;

        if (component.Cycleable)
        {
            args.Verbs.Add(new Verb()
            {
                Text = Loc.GetString("gun-ballistic-cycle"),
                Disabled = GetBallisticShots(component) == 0,
                Act = () => ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User),
            });

        }
    }

    private void OnBallisticExamine(EntityUid uid, BallisticAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", GetBallisticShots(component))));
    }

    private void ManualCycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates, EntityUid? user = null, GunComponent? gunComp = null)
    {
        if (!component.Cycleable)
            return;

        // Reset shotting for cycling
        if (Resolve(uid, ref gunComp, false) &&
            gunComp is { FireRateModified: > 0f } &&
            !Paused(uid))
        {
            gunComp.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1 / gunComp.FireRateModified);
            DirtyField(uid, gunComp, nameof(GunComponent.NextFire));
        }

        Audio.PlayPredicted(component.SoundRack, uid, user);

        var shots = GetBallisticShots(component);
        Cycle(uid, component, coordinates);

        var text = Loc.GetString(shots == 0 ? "gun-ballistic-cycled-empty" : "gun-ballistic-cycled");

        Popup(text, uid, user);
        UpdateBallisticAppearance(uid, component);
        UpdateAmmoCount(uid);
    }

    protected abstract void Cycle(EntityUid uid, BallisticAmmoProviderComponent component, MapCoordinates coordinates);

    private void OnBallisticInit(EntityUid uid, BallisticAmmoProviderComponent component, ComponentInit args)
    {
        component.Container = Containers.EnsureContainer<Container>(uid, "ballistic-ammo");
        // TODO: This is called twice though we need to support loading appearance data (and we need to call it on MapInit
        // to ensure it's correct).
        UpdateBallisticAppearance(uid, component);
    }

    private void OnBallisticMapInit(EntityUid uid, BallisticAmmoProviderComponent component, MapInitEvent args)
    {
        // TODO this should be part of the prototype, not set on map init.
        // Alternatively, just track spawned count, instead of unspawned count.
        if (component.Proto != null)
        {
            component.UnspawnedCount = Math.Max(0, component.Capacity - component.Container.ContainedEntities.Count);
            UpdateBallisticAppearance(uid, component);
            DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
        }
    }

    protected int GetBallisticShots(BallisticAmmoProviderComponent component)
    {
        return component.Entities.Count + component.UnspawnedCount;
    }

    private void OnBallisticTakeAmmo(EntityUid uid, BallisticAmmoProviderComponent component, TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            EntityUid? ammoEntity = null;
            if (component.Entities.Count > 0)
            {
                var existingEnt = component.Entities[^1];
                component.Entities.RemoveAt(component.Entities.Count - 1);
                DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.Entities));
                Containers.Remove(existingEnt, component.Container);
                ammoEntity = existingEnt;
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                DirtyField(uid, component, nameof(BallisticAmmoProviderComponent.UnspawnedCount));
                ammoEntity = Spawn(component.Proto, args.Coordinates);
            }

            if (ammoEntity is { } ent)
            {
                args.Ammo.Add((ent, EnsureShootable(ent)));
                if (TryComp<BallisticAmmoSelfRefillerComponent>(uid, out var refiller))
                {
                    PauseSelfRefill((uid, refiller));
                }
            }
        }

        UpdateBallisticAppearance(uid, component);
    }

    private void OnBallisticAmmoCount(EntityUid uid, BallisticAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = GetBallisticShots(component);
        args.Capacity = component.Capacity;
    }

    /// <summary>
    /// Causes <paramref name="entity"/> to pause its refilling for either at least <paramref name="overridePauseDuration"/>
    /// (if not null) or the entity's <see cref="BallisticAmmoSelfRefillerComponent.AutoRefillPauseDuration"/>. If the
    /// entity's next refill would occur after the pause duration, this function has no effect.
    /// </summary>
    public void PauseSelfRefill(
        Entity<BallisticAmmoSelfRefillerComponent> entity,
        TimeSpan? overridePauseDuration = null
    )
    {
        if (overridePauseDuration == null && !entity.Comp.FiringPausesAutoRefill)
            return;

        var nextRefillByPause = Timing.CurTime + (overridePauseDuration ?? entity.Comp.AutoRefillPauseDuration);
        if (nextRefillByPause > entity.Comp.NextAutoRefill)
        {
            entity.Comp.NextAutoRefill = nextRefillByPause;
            DirtyField(entity.AsNullable(), nameof(BallisticAmmoSelfRefillerComponent.NextAutoRefill));
        }
    }

    /// <summary>
    /// Returns true if the given <paramref name="entity"/>'s ballistic ammunition is full, false otherwise.
    /// </summary>
    public bool IsFull(Entity<BallisticAmmoProviderComponent> entity)
    {
        return GetBallisticShots(entity.Comp) >= entity.Comp.Capacity;
    }

    /// <summary>
    /// Returns whether or not <paramref name="inserted"/> can be inserted into <paramref name="entity"/>, based on
    /// available space and whitelists.
    /// </summary>
    public bool CanInsertBallistic(Entity<BallisticAmmoProviderComponent> entity, EntityUid inserted)
    {
        return !_whitelistSystem.IsWhitelistFailOrNull(entity.Comp.Whitelist, inserted) &&
               !IsFull(entity);
    }

    /// <summary>
    /// Attempts to insert <paramref name="inserted"/> into <paramref name="entity"/> as ammunition. Returns true on
    /// success, false otherwise.
    /// </summary>
    public bool TryBallisticInsert(
        Entity<BallisticAmmoProviderComponent> entity,
        EntityUid inserted,
        EntityUid? user,
        bool suppressInsertionSound = false
    )
    {
        if (!CanInsertBallistic(entity, inserted))
            return false;

        entity.Comp.Entities.Add(inserted);
        Containers.Insert(inserted, entity.Comp.Container);
        if (!suppressInsertionSound)
        {
            Audio.PlayPredicted(entity.Comp.SoundInsert, entity, user);
        }

        UpdateBallisticAppearance(entity, entity.Comp);
        UpdateAmmoCount(entity);
        DirtyField(entity.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));

        return true;
    }

    public void UpdateBallisticAppearance(EntityUid uid, BallisticAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        Appearance.SetData(uid, AmmoVisuals.AmmoCount, GetBallisticShots(component), appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.Capacity, appearance);
    }

    public void SetBallisticUnspawned(Entity<BallisticAmmoProviderComponent> entity, int count)
    {
        if (entity.Comp.UnspawnedCount == count)
            return;

        entity.Comp.UnspawnedCount = count;
        UpdateBallisticAppearance(entity.Owner, entity.Comp);
        UpdateAmmoCount(entity.Owner);
        Dirty(entity);
    }

    private void OnRefillerEmpPulsed(Entity<BallisticAmmoSelfRefillerComponent> entity, ref EmpPulseEvent args)
    {
        if (!entity.Comp.AffectedByEmp)
            return;

        PauseSelfRefill(entity, args.Duration);
    }

    private void UpdateBallistic(float frameTime)
    {
        var query = EntityQueryEnumerator<BallisticAmmoSelfRefillerComponent, BallisticAmmoProviderComponent>();
        while (query.MoveNext(out var uid, out var refiller, out var ammo))
        {
            BallisticSelfRefillerUpdate((uid, ammo, refiller));
        }
    }

    private void BallisticSelfRefillerUpdate(
        Entity<BallisticAmmoProviderComponent, BallisticAmmoSelfRefillerComponent> entity
    )
    {
        var ammo = entity.Comp1;
        var refiller = entity.Comp2;
        if (Timing.CurTime < refiller.NextAutoRefill)
            return;

        refiller.NextAutoRefill += refiller.AutoRefillRate;
        DirtyField(entity, refiller, nameof(BallisticAmmoSelfRefillerComponent.NextAutoRefill));

        if (!refiller.AutoRefill || IsFull(entity))
            return;

        if (refiller.AmmoProto is not { } refillerAmmoProto)
        {
            // No ammo proto on the refiller, so just increment the unspawned count on the provider
            // if it has an ammo proto.
            if (ammo.Proto is null)
            {
                Log.Error(
                    $"Neither of {entity}'s {nameof(BallisticAmmoSelfRefillerComponent)}'s or {nameof(BallisticAmmoProviderComponent)}'s ammunition protos is specified. This is a configuration error as it means {nameof(BallisticAmmoSelfRefillerComponent)} cannot do anything.");
                return;
            }

            SetBallisticUnspawned(entity, ammo.UnspawnedCount + 1);
        }
        else if (ammo.Proto == refillerAmmoProto)
        {
            // The ammo proto on the refiller and the provider match. Add an unspawned ammo.
            SetBallisticUnspawned(entity, ammo.UnspawnedCount + 1);
        }
        else
        {
            // Can't use unspawned ammo, so spawn an entity and try to insert it.
            var ammoEntity = PredictedSpawnAttachedTo(refiller.AmmoProto, Transform(entity).Coordinates);
            var insertSucceeded = TryBallisticInsert(entity, ammoEntity, null, suppressInsertionSound: true);
            if (!insertSucceeded)
            {
                PredictedQueueDel(ammoEntity);
                Log.Error(
                    $"Failed to insert ammo {ammoEntity} into non-full {entity}. This is a configuration error. Is the {nameof(BallisticAmmoSelfRefillerComponent)}'s {nameof(BallisticAmmoSelfRefillerComponent.AmmoProto)} incorrect for the {nameof(BallisticAmmoProviderComponent)}'s {nameof(BallisticAmmoProviderComponent.Whitelist)}?");
            }
        }
    }
}

/// <summary>
/// DoAfter event for filling one ballistic ammo provider from another.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AmmoFillDoAfterEvent : SimpleDoAfterEvent
{
}
