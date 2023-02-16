using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    protected virtual void InitializeBallistic()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentInit>(OnBallisticInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, MapInitEvent>(OnBallisticMapInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, TakeAmmoEvent>(OnBallisticTakeAmmo);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetAmmoCountEvent>(OnBallisticAmmoCount);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentGetState>(OnBallisticGetState);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentHandleState>(OnBallisticHandleState);

        SubscribeLocalEvent<BallisticAmmoProviderComponent, ExaminedEvent>(OnBallisticExamine);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, GetVerbsEvent<Verb>>(OnBallisticVerb);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, InteractUsingEvent>(OnBallisticInteractUsing);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, AfterInteractEvent>(OnBallisticAfterInteract);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, UseInHandEvent>(OnBallisticUse);
    }

    private void OnBallisticUse(EntityUid uid, BallisticAmmoProviderComponent component, UseInHandEvent args)
    {
        ManualCycle(component, Transform(uid).MapPosition, args.User);
        args.Handled = true;
    }

    private void OnBallisticInteractUsing(EntityUid uid, BallisticAmmoProviderComponent component, InteractUsingEvent args)
    {
        if (args.Handled || component.Whitelist?.IsValid(args.Used, EntityManager) != true) return;

        if (GetBallisticShots(component) >= component.Capacity) return;

        component.Entities.Add(args.Used);
        component.Container.Insert(args.Used);
        // Not predicted so
        Audio.PlayPredicted(component.SoundInsert, uid, args.User);
        args.Handled = true;
        UpdateBallisticAppearance(component);
        Dirty(component);
    }

    private void OnBallisticAfterInteract(EntityUid uid, BallisticAmmoProviderComponent component, AfterInteractEvent args)
    {
        if (args.Handled ||
            !component.MayTransfer ||
            !Timing.IsFirstTimePredicted ||
            args.Target == null ||
            args.Used == args.Target ||
            Deleted(args.Target) ||
            !TryComp(args.Target, out BallisticAmmoProviderComponent? targetComponent) ||
            targetComponent.Whitelist == null)
        {
            return;
        }

        args.Handled = true;

        if (targetComponent.Entities.Count + targetComponent.UnspawnedCount == targetComponent.Capacity)
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
                    ("entity", args.Used)),
                args.Used,
                args.User);
            return;
        }

        void SimulateInsertAmmo(EntityUid ammo, EntityUid ammoProvider, EntityCoordinates coordinates)
        {
            var evInsert = new InteractUsingEvent(args.User, ammo, ammoProvider, coordinates);
            RaiseLocalEvent(ammoProvider, evInsert);
        }

        List<IShootable> ammo = new();
        var evTakeAmmo = new TakeAmmoEvent(1, ammo, Transform(args.Used).Coordinates, args.User);
        RaiseLocalEvent(args.Used, evTakeAmmo);

        foreach (var shot in ammo)
        {
            if (shot is not AmmoComponent cast)
                continue;

            if (!targetComponent.Whitelist.IsValid(cast.Owner))
            {
                Popup(
                    Loc.GetString("gun-ballistic-transfer-invalid",
                        ("ammoEntity", cast.Owner),
                        ("targetEntity", args.Target.Value)),
                    args.Used,
                    args.User);

                // TODO: For better or worse, this will play a sound, but it's the
                // more future-proof thing to do than copying the same code
                // that OnBallisticInteractUsing has, sans sound.
                SimulateInsertAmmo(cast.Owner, args.Used, Transform(args.Used).Coordinates);
            }
            else
            {
                SimulateInsertAmmo(cast.Owner, args.Target.Value, Transform(args.Target.Value).Coordinates);
            }

            if (cast.Owner.IsClientSide())
                Del(cast.Owner);
        }
    }

    private void OnBallisticVerb(EntityUid uid, BallisticAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null) return;

        args.Verbs.Add(new Verb()
        {
            Text = Loc.GetString("gun-ballistic-cycle"),
            Disabled = GetBallisticShots(component) == 0,
            Act = () => ManualCycle(component, Transform(uid).MapPosition, args.User),
        });
    }

    private void OnBallisticExamine(EntityUid uid, BallisticAmmoProviderComponent component, ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("gun-magazine-examine", ("color", AmmoExamineColor), ("count", GetBallisticShots(component))));
    }

    private void ManualCycle(BallisticAmmoProviderComponent component, MapCoordinates coordinates, EntityUid? user = null)
    {
        // Reset shotting for cycling
        if (TryComp<GunComponent>(component.Owner, out var gunComp) &&
            gunComp is { FireRate: > 0f })
        {
            gunComp.NextFire = Timing.CurTime + TimeSpan.FromSeconds(1 / gunComp.FireRate);
        }

        Dirty(component);
        Audio.PlayPredicted(component.SoundRack, component.Owner, user);

        var shots = GetBallisticShots(component);
        component.Cycled = true;

        Cycle(component, coordinates);

        var text = Loc.GetString(shots == 0 ? "gun-ballistic-cycled-empty" : "gun-ballistic-cycled");

        Popup(text, component.Owner, user);
        UpdateBallisticAppearance(component);
        UpdateAmmoCount(component.Owner);
    }

    protected abstract void Cycle(BallisticAmmoProviderComponent component, MapCoordinates coordinates);

    private void OnBallisticGetState(EntityUid uid, BallisticAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new BallisticAmmoProviderComponentState()
        {
            UnspawnedCount = component.UnspawnedCount,
            Entities = component.Entities,
            Cycled = component.Cycled,
        };
    }

    private void OnBallisticHandleState(EntityUid uid, BallisticAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not BallisticAmmoProviderComponentState state) return;

        component.Cycled = state.Cycled;
        component.UnspawnedCount = state.UnspawnedCount;

        component.Entities.Clear();

        foreach (var ent in state.Entities)
        {
            component.Entities.Add(ent);
        }
    }

    private void OnBallisticInit(EntityUid uid, BallisticAmmoProviderComponent component, ComponentInit args)
    {
        component.Container = Containers.EnsureContainer<Container>(uid, "ballistic-ammo");
    }

    private void OnBallisticMapInit(EntityUid uid, BallisticAmmoProviderComponent component, MapInitEvent args)
    {
        // TODO this should be part of the prototype, not set on map init.
        // Alternatively, just track spawned count, instead of unspawned count.
        if (component.FillProto != null)
        {
            component.UnspawnedCount = Math.Max(0, component.Capacity - component.Container.ContainedEntities.Count);
            Dirty(component);
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
            if (!component.Cycled) break;

            EntityUid entity;

            if (component.Entities.Count > 0)
            {
                entity = component.Entities[^1];

                args.Ammo.Add(EnsureComp<AmmoComponent>(entity));

                // Leave the entity as is if it doesn't auto cycle
                // TODO: Suss this out with NewAmmoComponent as I don't think it gets removed from container properly
                if (!component.AutoCycle)
                {
                    return;
                }

                component.Entities.RemoveAt(component.Entities.Count - 1);
                component.Container.Remove(entity);
            }
            else if (component.UnspawnedCount > 0)
            {
                component.UnspawnedCount--;
                entity = Spawn(component.FillProto, args.Coordinates);
                args.Ammo.Add(EnsureComp<AmmoComponent>(entity));

                // Put it back in if it doesn't auto-cycle
                if (HasComp<CartridgeAmmoComponent>(entity) && !component.AutoCycle)
                {
                    if (!entity.IsClientSide())
                    {
                        component.Entities.Add(entity);
                        component.Container.Insert(entity);
                    }
                    else
                    {
                        component.UnspawnedCount++;
                    }
                }
            }

            if (!component.AutoCycle)
            {
                component.Cycled = false;
            }
        }

        UpdateBallisticAppearance(component);
        Dirty(component);
    }

    private void OnBallisticAmmoCount(EntityUid uid, BallisticAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = GetBallisticShots(component);
        args.Capacity = component.Capacity;
    }

    private void UpdateBallisticAppearance(BallisticAmmoProviderComponent component)
    {
        if (!Timing.IsFirstTimePredicted || !TryComp<AppearanceComponent>(component.Owner, out var appearance))
            return;

        Appearance.SetData(appearance.Owner, AmmoVisuals.AmmoCount, GetBallisticShots(component), appearance);
        Appearance.SetData(appearance.Owner, AmmoVisuals.AmmoMax, component.Capacity, appearance);
    }

    [Serializable, NetSerializable]
    private sealed class BallisticAmmoProviderComponentState : ComponentState
    {
        public int UnspawnedCount;
        public List<EntityUid> Entities = default!;
        public bool Cycled;
    }
}
