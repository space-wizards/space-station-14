using Content.Shared.Interaction;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using System;
using System.Linq;
using Content.Shared.Interaction.Events;
using JetBrains.Annotations;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    protected const string RevolverContainer = "revolver-ammo";

    protected virtual void InitializeRevolver()
    {
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentGetState>(OnRevolverGetState);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentHandleState>(OnRevolverHandleState);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentInit>(OnRevolverInit);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, TakeAmmoEvent>(OnRevolverTakeAmmo);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, GetVerbsEvent<AlternativeVerb>>(OnRevolverVerbs);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, InteractUsingEvent>(OnRevolverInteractUsing);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, GetAmmoCountEvent>(OnRevolverGetAmmoCount);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, UseInHandEvent>(OnRevolverUse);
    }

    private void OnRevolverUse(EntityUid uid, RevolverAmmoProviderComponent component, UseInHandEvent args)
    {
        if (!_useDelay.TryResetDelay(uid))
            return;

        Cycle(component);
        UpdateAmmoCount(uid, prediction: false);
        Dirty(uid, component);
    }

    private void OnRevolverGetAmmoCount(EntityUid uid, RevolverAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count += GetRevolverCount(component);
        args.Capacity += component.Capacity;
    }

    private void OnRevolverInteractUsing(EntityUid uid, RevolverAmmoProviderComponent component, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryRevolverInsert(uid, component, args.Used, args.User))
            args.Handled = true;
    }

    private void OnRevolverGetState(EntityUid uid, RevolverAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new RevolverAmmoProviderComponentState
        {
            CurrentIndex = component.CurrentIndex,
            AmmoSlots = GetNetEntityList(component.AmmoSlots),
            Chambers = component.Chambers,
        };
    }

    private void OnRevolverHandleState(EntityUid uid, RevolverAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RevolverAmmoProviderComponentState state)
            return;

        var oldIndex = component.CurrentIndex;
        component.CurrentIndex = state.CurrentIndex;
        component.Chambers = new bool?[state.Chambers.Length];

        // Need to copy across the state rather than the ref.
        for (var i = 0; i < component.AmmoSlots.Count; i++)
        {
            component.AmmoSlots[i] = EnsureEntity<RevolverAmmoProviderComponent>(state.AmmoSlots[i], uid);
            component.Chambers[i] = state.Chambers[i];
        }

        // Handle spins
        if (oldIndex != state.CurrentIndex)
        {
            UpdateAmmoCount(uid, prediction: false);
        }
    }

    public bool TryRevolverInsert(EntityUid revolverUid, RevolverAmmoProviderComponent component, EntityUid uid, EntityUid? user)
    {
        if (component.Whitelist?.IsValid(uid, EntityManager) == false)
            return false;

        // If it's a speedloader try to get ammo from it.
        if (EntityManager.HasComponent<SpeedLoaderComponent>(uid))
        {
            var freeSlots = 0;

            for (var i = 0; i < component.Capacity; i++)
            {
                if (component.AmmoSlots[i] != null || component.Chambers[i] != null)
                    continue;

                freeSlots++;
            }

            if (freeSlots == 0)
            {
                Popup(Loc.GetString("gun-revolver-full"), revolverUid, user);
                return false;
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(uid);
            var ammo = new List<(EntityUid? Entity, IShootable Shootable)>(freeSlots);
            var ev = new TakeAmmoEvent(freeSlots, ammo, xform.Coordinates, user);
            RaiseLocalEvent(uid, ev);

            if (ev.Ammo.Count == 0)
            {
                Popup(Loc.GetString("gun-speedloader-empty"), revolverUid, user);
                return false;
            }

            for (var i = Math.Min(ev.Ammo.Count - 1, component.Capacity - 1); i >= 0; i--)
            {
                var index = (component.CurrentIndex + i) % component.Capacity;

                if (component.AmmoSlots[index] != null ||
                    component.Chambers[index] != null)
                {
                    continue;
                }

                var ent = ev.Ammo.Last().Entity;
                ev.Ammo.RemoveAt(ev.Ammo.Count - 1);

                if (ent == null)
                {
                    Log.Error($"Tried to load hitscan into a revolver which is unsupported");
                    continue;
                }

                component.AmmoSlots[index] = ent.Value;
                Containers.Insert(ent.Value, component.AmmoContainer);
                SetChamber(index, component, uid);

                if (ev.Ammo.Count == 0)
                    break;
            }

            DebugTools.Assert(ammo.Count == 0);
            UpdateRevolverAppearance(revolverUid, component);
            UpdateAmmoCount(revolverUid);
            Dirty(revolverUid, component);

            Audio.PlayPredicted(component.SoundInsert, revolverUid, user);
            Popup(Loc.GetString("gun-revolver-insert"), revolverUid, user);
            return true;
        }

        // Try to insert the entity directly.
        for (var i = 0; i < component.Capacity; i++)
        {
            var index = (component.CurrentIndex + i) % component.Capacity;

            if (component.AmmoSlots[index] != null ||
                component.Chambers[index] != null)
            {
                continue;
            }

            component.AmmoSlots[index] = uid;
            Containers.Insert(uid, component.AmmoContainer);
            SetChamber(index, component, uid);
            Audio.PlayPredicted(component.SoundInsert, revolverUid, user);
            Popup(Loc.GetString("gun-revolver-insert"), revolverUid, user);
            UpdateRevolverAppearance(revolverUid, component);
            UpdateAmmoCount(revolverUid);
            Dirty(revolverUid, component);
            return true;
        }

        Popup(Loc.GetString("gun-revolver-full"), revolverUid, user);
        return false;
    }

    private void SetChamber(int index, RevolverAmmoProviderComponent component, EntityUid uid)
    {
        if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge) && cartridge.Spent)
        {
            component.Chambers[index] = false;
            return;
        }

        component.Chambers[index] = true;
    }

    private void OnRevolverVerbs(EntityUid uid, RevolverAmmoProviderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("gun-revolver-empty"),
            Disabled = !AnyRevolverCartridges(component),
            Act = () => EmptyRevolver(uid, component, args.User),
            Priority = 1
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("gun-revolver-spin"),
            // Category = VerbCategory.G,
            Act = () => SpinRevolver(uid, component, args.User)
        });
    }

    private bool AnyRevolverCartridges(RevolverAmmoProviderComponent component)
    {
        for (var i = 0; i < component.Capacity; i++)
        {
            if (component.Chambers[i] != null ||
                component.AmmoSlots[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private int GetRevolverCount(RevolverAmmoProviderComponent component)
    {
        var count = 0;

        for (var i = 0; i < component.Capacity; i++)
        {
            if (component.Chambers[i] != null ||
                component.AmmoSlots[i] != null)
            {
                count++;
            }
        }

        return count;
    }

    [PublicAPI]
    private int GetRevolverUnspentCount(RevolverAmmoProviderComponent component)
    {
        var count = 0;

        for (var i = 0; i < component.Capacity; i++)
        {
            var chamber = component.Chambers[i];

            if (chamber == true)
            {
                count++;
                continue;
            }

            var ammo = component.AmmoSlots[i];

            if (TryComp<CartridgeAmmoComponent>(ammo, out var cartridge) && !cartridge.Spent)
            {
                count++;
            }
        }

        return count;
    }

    public void EmptyRevolver(EntityUid revolverUid, RevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        var mapCoordinates = TransformSystem.GetMapCoordinates(revolverUid);
        var anyEmpty = false;

        for (var i = 0; i < component.Capacity; i++)
        {
            var chamber = component.Chambers[i];
            var slot = component.AmmoSlots[i];

            if (slot == null)
            {
                if (chamber == null)
                    continue;

                // Too lazy to make a new method don't sue me.
                if (!_netManager.IsClient)
                {
                    var uid = Spawn(component.FillPrototype, mapCoordinates);

                    if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
                        SetCartridgeSpent(uid, cartridge, !(bool) chamber);

                    EjectCartridge(uid);
                }

                component.Chambers[i] = null;
                anyEmpty = true;
            }
            else
            {
                component.AmmoSlots[i] = null;
                Containers.Remove(slot.Value, component.AmmoContainer);
                component.Chambers[i] = null;

                if (!_netManager.IsClient)
                    EjectCartridge(slot.Value);

                anyEmpty = true;
            }
        }

        if (anyEmpty)
        {
            Audio.PlayPredicted(component.SoundEject, revolverUid, user);
            UpdateAmmoCount(revolverUid, prediction: false);
            UpdateRevolverAppearance(revolverUid, component);
            Dirty(revolverUid, component);
        }
    }

    private void UpdateRevolverAppearance(EntityUid uid, RevolverAmmoProviderComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        var count = GetRevolverCount(component);
        Appearance.SetData(uid, AmmoVisuals.HasAmmo, count != 0, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoCount, count, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.Capacity, appearance);
    }

    protected virtual void SpinRevolver(EntityUid revolverUid, RevolverAmmoProviderComponent component, EntityUid? user = null)
    {
        Audio.PlayPredicted(component.SoundSpin, revolverUid, user);
        Popup(Loc.GetString("gun-revolver-spun"), revolverUid, user);
    }

    private void OnRevolverTakeAmmo(EntityUid uid, RevolverAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var currentIndex = component.CurrentIndex;
        Cycle(component, args.Shots);

        // Revolvers provide the bullets themselves rather than the cartridges so they stay in the revolver.
        for (var i = 0; i < args.Shots; i++)
        {
            var index = (currentIndex + i) % component.Capacity;
            var chamber = component.Chambers[index];
            EntityUid? ent = null;

            // Get contained entity if it exists.
            if (component.AmmoSlots[index] != null)
            {
                ent = component.AmmoSlots[index]!;
                component.Chambers[index] = false;
            }
            // Try to spawn a round if it's available.
            else if (chamber != null)
            {
                if (chamber == true)
                {
                    // Pretend it's always been there.
                    ent = Spawn(component.FillPrototype, args.Coordinates);

                    if (!_netManager.IsClient)
                    {
                        component.AmmoSlots[index] = ent;
                        Containers.Insert(ent.Value, component.AmmoContainer);
                    }

                    component.Chambers[index] = false;
                }
            }

            // Chamber empty or spent
            if (ent == null)
                continue;

            if (TryComp<CartridgeAmmoComponent>(ent, out var cartridge))
            {
                if (cartridge.Spent)
                    continue;

                // Mark cartridge as spent and if it's caseless delete from the chamber slot.
                SetCartridgeSpent(ent.Value, cartridge, true);
                var spawned = Spawn(cartridge.Prototype, args.Coordinates);
                args.Ammo.Add((spawned, EnsureComp<AmmoComponent>(spawned)));

                if (cartridge.DeleteOnSpawn)
                    component.Chambers[index] = null;
            }
            else
            {
                component.Chambers[index] = null;
                args.Ammo.Add((ent.Value, EnsureComp<AmmoComponent>(ent.Value)));
            }

            // Delete the cartridge entity on client
            if (_netManager.IsClient)
            {
                QueueDel(ent);
            }
        }

        UpdateAmmoCount(uid, prediction: false);
        UpdateRevolverAppearance(uid, component);
        Dirty(uid, component);
    }

    private void Cycle(RevolverAmmoProviderComponent component, int count = 1)
    {
        component.CurrentIndex = (component.CurrentIndex + count) % component.Capacity;
    }

    private void OnRevolverInit(EntityUid uid, RevolverAmmoProviderComponent component, ComponentInit args)
    {
        component.AmmoContainer = Containers.EnsureContainer<Container>(uid, RevolverContainer);
        component.AmmoSlots.EnsureCapacity(component.Capacity);
        var remainder = component.Capacity - component.AmmoSlots.Count;

        for (var i = 0; i < remainder; i++)
        {
            component.AmmoSlots.Add(null);
        }

        component.Chambers = new bool?[component.Capacity];

        if (component.FillPrototype != null)
        {
            for (var i = 0; i < component.Capacity; i++)
            {
                if (component.AmmoSlots[i] != null)
                {
                    component.Chambers[i] = null;
                    continue;
                }

                component.Chambers[i] = true;
            }
        }

        DebugTools.Assert(component.AmmoSlots.Count == component.Capacity);
    }

    [Serializable, NetSerializable]
    protected sealed class RevolverAmmoProviderComponentState : ComponentState
    {
        public int CurrentIndex;
        public List<NetEntity?> AmmoSlots = default!;
        public bool?[] Chambers = default!;
    }

    public sealed class RevolverSpinEvent : EntityEventArgs
    {

    }
}
