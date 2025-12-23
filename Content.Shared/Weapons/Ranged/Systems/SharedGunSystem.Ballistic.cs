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
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;


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
    }

    private void OnBallisticUse(Entity<BallisticAmmoProviderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        ManualCycle(ent, TransformSystem.GetMapCoordinates(ent), args.User);
        args.Handled = true;
    }

    private void OnBallisticInteractUsing(Entity<BallisticAmmoProviderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (_whitelistSystem.IsWhitelistFailOrNull(ent.Comp.Whitelist, args.Used))
            return;

        if (GetBallisticShots(ent.Comp) >= ent.Comp.Capacity)
            return;

        ent.Comp.Entities.Add(args.Used);
        Containers.Insert(args.Used, ent.Comp.Container);
        // Not predicted so
        Audio.PlayPredicted(ent.Comp.SoundInsert, ent, args.User);
        args.Handled = true;
        UpdateBallisticAppearance(ent);
        UpdateAmmoCount(args.Target);
        DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));
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
                Act = () => ManualCycle((uid, component), TransformSystem.GetMapCoordinates(uid), args.User),
            });

        }
    }

    private void OnBallisticExamine(EntityUid uid, BallisticAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", GetBallisticShots(component))));
    }

    private void ManualCycle(Entity<BallisticAmmoProviderComponent> ent, MapCoordinates coordinates, EntityUid? user = null, GunComponent? gunComp = null)
    {
        if (!ent.Comp.Cycleable)
            return;

        // Reset shotting for cycling
        if (Resolve(ent, ref gunComp, false) &&
            gunComp is { FireRateModified: > 0f } &&
            !Paused(ent))
        {
            gunComp.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1 / gunComp.FireRateModified);
            DirtyField(ent, gunComp, nameof(GunComponent.NextFire));
        }

        Audio.PlayPredicted(ent.Comp.SoundRack, ent, user);

        var shots = GetBallisticShots(ent.Comp);
        Cycle(ent, coordinates);

        var text = Loc.GetString(shots == 0 ? "gun-ballistic-cycled-empty" : "gun-ballistic-cycled");

        Popup(text, ent, user);
        UpdateBallisticAppearance(ent);
        UpdateAmmoCount(ent);
    }

    protected abstract void Cycle(Entity<BallisticAmmoProviderComponent> ent, MapCoordinates coordinates);

    private void OnBallisticInit(Entity<BallisticAmmoProviderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Container = Containers.EnsureContainer<Container>(ent, "ballistic-ammo");
        // TODO: This is called twice though we need to support loading appearance data (and we need to call it on MapInit
        // to ensure it's correct).
        UpdateBallisticAppearance(ent);
    }

    private void OnBallisticMapInit(Entity<BallisticAmmoProviderComponent> ent, ref MapInitEvent args)
    {
        // TODO this should be part of the prototype, not set on map init.
        // Alternatively, just track spawned count, instead of unspawned count.
        if (ent.Comp.Proto != null)
        {
            ent.Comp.UnspawnedCount = Math.Max(0, ent.Comp.Capacity - ent.Comp.Container.ContainedEntities.Count);
            UpdateBallisticAppearance(ent);
            DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.UnspawnedCount));
        }
    }

    protected int GetBallisticShots(BallisticAmmoProviderComponent component)
    {
        return component.Entities.Count + component.UnspawnedCount;
    }

    private void OnBallisticTakeAmmo(Entity<BallisticAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        for (var i = 0; i < args.Shots; i++)
        {
            EntityUid ammoEnt;

            if (ent.Comp.Entities.Count > 0)
            {
                ammoEnt = ent.Comp.Entities[^1];

                args.Ammo.Add((ammoEnt, EnsureShootable(ammoEnt)));
                ent.Comp.Entities.RemoveAt(ent.Comp.Entities.Count - 1);
                DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.Entities));
                Containers.Remove(ammoEnt, ent.Comp.Container);
            }
            else if (ent.Comp.UnspawnedCount > 0)
            {
                ent.Comp.UnspawnedCount--;
                DirtyField(ent.AsNullable(), nameof(BallisticAmmoProviderComponent.UnspawnedCount));
                ammoEnt = Spawn(ent.Comp.Proto, args.Coordinates);
                args.Ammo.Add((ammoEnt, EnsureShootable(ammoEnt)));
            }
        }

        UpdateBallisticAppearance(ent);
    }

    private void OnBallisticAmmoCount(EntityUid uid, BallisticAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = GetBallisticShots(component);
        args.Capacity = component.Capacity;
    }

    public void UpdateBallisticAppearance(Entity<BallisticAmmoProviderComponent> ent)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        Appearance.SetData(ent, AmmoVisuals.AmmoCount, GetBallisticShots(ent.Comp), appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.Capacity, appearance);
    }

    public void SetBallisticUnspawned(Entity<BallisticAmmoProviderComponent> entity, int count)
    {
        if (entity.Comp.UnspawnedCount == count)
            return;

        entity.Comp.UnspawnedCount = count;
        UpdateBallisticAppearance(entity);
        UpdateAmmoCount(entity.Owner);
        Dirty(entity);
    }
}

/// <summary>
/// DoAfter event for filling one ballistic ammo provider from another.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class AmmoFillDoAfterEvent : SimpleDoAfterEvent
{
}
