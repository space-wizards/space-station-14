using Content.Shared.DoAfter;
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
        SubscribeLocalEvent<BallisticAmmoProviderComponent, DelayedAmmoInsertDoAfterEvent>(OnBallisticDelayedAmmoInsertDoAfter);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, DelayedCycleDoAfterEvent>(OnBallisticDelayedCycleDoAfter);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, UseInHandEvent>(OnBallisticUse);
    }

    /// <summary>
    ///  Use in hand. Calls ManualCycle to remove a round if component.Cycleable is true.
    ///  Separate because ManualCycle can also be called by the get-ballistic-cycle verb.
    ///  Using the weapon in hand is much faster than using the verb, but the verb still works even if that's overridden.
    /// </summary>
    private void OnBallisticUse(EntityUid uid, BallisticAmmoProviderComponent component, UseInHandEvent args)
    {
        if (args.Handled || !component.Cycleable)
            return;

        if (component.CycleDelay > 0)
            BallisticCycleDelayCheck(uid, component, args.User);
        else // If there's no custom CycleDelay on this component by default, immediately cycle.
            ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User);

        args.Handled = true;
    }

    private void BallisticCycleDelayCheck(EntityUid uid, BallisticAmmoProviderComponent component, EntityUid user)
    {
        // Handling cycleDelay and DoAfter here sinceboth UseInHandEvent and Verb need to do these checks.
        TimeSpan cycleDelayConverted;

        if (component.CycleDelay > 0)
        {
            Popup(
                Loc.GetString("gun-ballistic-cycle-delayed",
                    ("entity", uid)),
                    uid,
                    user);

            cycleDelayConverted = TimeSpan.FromSeconds(component.CycleDelay);
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, cycleDelayConverted, new DelayedCycleDoAfterEvent(), used: uid, target: uid, eventTarget: uid)
            {
                BreakOnMove = true,
                BreakOnDamage = false,
                NeedHand = true
            });
        }
        else
            ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), user);
    }

    /// <summary>
    /// Interact with a BallisticAmmoProvider using something else in hand, usually to load it with loose cartridges or other ammo.
    /// Includes both magazines and some guns that take ammo directly, like shotguns and launchers.
    /// Uses InsertDelay instead of FillDelay, which defaults to 0. InsertDelay > 0 makes loading a DoAfter channel, even with Ammo components.
    /// If transferring from another BallisticAmmoProvider, OnBallisticAfterInteract takes precedence and uses FillDelay instead.
    /// </summary>
    private void OnBallisticInteractUsing(EntityUid uid, BallisticAmmoProviderComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFailOrNull(component.Whitelist, args.Used))
            return;

        if (GetBallisticShots(component) >= component.Capacity)
            return;

        TimeSpan insertDelayConverted;

        if (component.InsertDelay > 0)
        {
            insertDelayConverted = TimeSpan.FromSeconds(component.InsertDelay);
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, insertDelayConverted, new DelayedAmmoInsertDoAfterEvent(), used: args.Used, target: args.Target, eventTarget: uid)
            {
                BreakOnMove = true,
                BreakOnDamage = false,
                NeedHand = true
            });
        }
        else // If there's no custom InsertDelay on this component, immediately load the ammo in. Shotguns and mag-filling.
        {
            ManualLoad(uid, component, args.Used, args.User);
        }
        args.Handled = true;
    }

    /// <summary>
    /// Interacting with a BallisticAmmoProvider with another one, to transfer ammo.
    /// Uses FillDelay, defaulting to 0.5s
    /// </summary>
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

        TimeSpan fillDelayConverted = TimeSpan.FromSeconds(component.FillDelay);

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, fillDelayConverted, new AmmoFillDoAfterEvent(), used: uid, target: args.Target, eventTarget: uid)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            NeedHand = true
        });
    }

    private void OnBallisticAmmoFillDoAfter(EntityUid uid, BallisticAmmoProviderComponent component, AmmoFillDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        if (Deleted(args.Target) ||
            !TryComp<BallisticAmmoProviderComponent>(args.Target, out var target) ||
            target.Whitelist == null ||
            args.Cancelled)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-cancelled",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }

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
        // Simulates using a single ammo entity on the other BAP, loading it in.
        void SimulateInsertAmmo(EntityUid ammo, EntityUid ammoProvider, EntityCoordinates coordinates)
        {
            var evInsert = new InteractUsingEvent(args.User, ammo, ammoProvider, coordinates);
            RaiseLocalEvent(ammoProvider, evInsert);
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

        // repeat if there is more space in the target and more ammo to fill it
        var moreSpace = target.Entities.Count + target.UnspawnedCount < target.Capacity;
        var moreAmmo = component.Entities.Count + component.UnspawnedCount > 0;
        args.Repeat = moreSpace && moreAmmo;

        // Delete the source BAP if it has the flag and is empty after trying to load. Maybe useful for shell handfuls.
        if (component.DeleteWhenEmpty && (component.Entities.Count == 0))
            Del(uid);

    }

    private void OnBallisticDelayedAmmoInsertDoAfter(EntityUid uid, BallisticAmmoProviderComponent component, DelayedAmmoInsertDoAfterEvent args)
    {
        // Check the DoAfter wasn't cancelled and nothing's missing.
        if (Deleted(args.Target) ||
            !TryComp<BallisticAmmoProviderComponent>(args.Target, out var target) ||
            target.Whitelist == null ||
            args.Cancelled)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-cancelled",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }
        // Check if full.
        if (target.Entities.Count + target.UnspawnedCount == target.Capacity)
        {
            Popup(
                Loc.GetString("gun-ballistic-transfer-target-full",
                    ("entity", args.Target)),
                args.Target,
                args.User);
            return;
        }

        if (args.Used is not null)
        {
            ManualLoad(uid, component, args.Used.Value, args.User);
            args.Handled = true;
            return;
        }
        args.Handled = false;
    }

    private void ManualLoad(EntityUid uid, BallisticAmmoProviderComponent component, EntityUid used, EntityUid user)
    {
        // Reused function moved here.
        component.Entities.Add(used);
        Containers.Insert(used, component.Container);
        // Not predicted so
        Audio.PlayPredicted(component.SoundInsert, uid, user);
        UpdateBallisticAppearance(uid, component);
        Dirty(uid, component);
        return;
    }

    private void OnBallisticDelayedCycleDoAfter(EntityUid uid, BallisticAmmoProviderComponent component, DelayedCycleDoAfterEvent args)
    {
        // Check the DoAfter wasn't interrupted and the target BAP still exists.
        if (Deleted(uid) ||
            args.Cancelled)
        {
            Popup(
                Loc.GetString("gun-ballistic-cycle-delayed-cancelled",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }
        // Check if empty.
        if (component.Entities.Count + component.UnspawnedCount == 0)
        {
            Popup(
                Loc.GetString("gun-ballistic-cycle-delayed-empty",
                    ("entity", uid)),
                uid,
                args.User);
            return;
        }

        ManualCycle(uid, component, TransformSystem.GetMapCoordinates(uid), args.User);

        args.Handled = true;
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
                Disabled = (GetBallisticShots(component) == 0),
                Act = () => BallisticCycleDelayCheck(uid, component, args.User),
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
            Dirty(uid, gunComp);
        }

        Dirty(uid, component);
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
            Dirty(uid, component);
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
            EntityUid entity;

            if (component.Entities.Count > 0)
            {
                entity = component.Entities[^1];

                args.Ammo.Add((entity, EnsureShootable(entity)));
                component.Entities.RemoveAt(component.Entities.Count - 1);
                Containers.Remove(entity, component.Container);
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                entity = Spawn(component.Proto, args.Coordinates);
                args.Ammo.Add((entity, EnsureShootable(entity)));
            }
        }

        UpdateBallisticAppearance(uid, component);
        Dirty(uid, component);
    }

    private void OnBallisticAmmoCount(EntityUid uid, BallisticAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = GetBallisticShots(component);
        args.Capacity = component.Capacity;
    }

    public void UpdateBallisticAppearance(EntityUid uid, BallisticAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        Appearance.SetData(uid, AmmoVisuals.AmmoCount, GetBallisticShots(component), appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.Capacity, appearance);
    }
}

/// <summary>
/// DoAfter event for filling one ballistic ammo provider from another.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AmmoFillDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// DoAfter event for filling a ballistic ammo provider directly while InsertDelay > 0.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DelayedAmmoInsertDoAfterEvent : SimpleDoAfterEvent
{
}

/// <summary>
/// DoAfter event for cycling a ballistic ammo provider while CycleDelay > 0.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class DelayedCycleDoAfterEvent : SimpleDoAfterEvent
{
}
