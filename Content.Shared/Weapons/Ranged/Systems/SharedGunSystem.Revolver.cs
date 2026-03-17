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
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;
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

    private void OnRevolverUse(Entity<RevolverAmmoProviderComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (!_useDelay.TryResetDelay(ent))
            return;

        args.Handled = true;

        Cycle(ent.Comp);
        UpdateAmmoCount(ent, prediction: false);
        Dirty(ent);
    }

    private void OnRevolverGetAmmoCount(Entity<RevolverAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count += GetRevolverCount(ent.Comp);
        args.Capacity += ent.Comp.Capacity;
    }

    private void OnRevolverInteractUsing(Entity<RevolverAmmoProviderComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        if (TryRevolverInsert(ent, args.Used, args.User))
            args.Handled = true;
    }

    private void OnRevolverGetState(Entity<RevolverAmmoProviderComponent> ent, ref ComponentGetState args)
    {
        args.State = new RevolverAmmoProviderComponentState
        {
            CurrentIndex = ent.Comp.CurrentIndex,
            AmmoSlots = GetNetEntityList(ent.Comp.AmmoSlots),
            Chambers = ent.Comp.Chambers,
        };
    }

    private void OnRevolverHandleState(Entity<RevolverAmmoProviderComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not RevolverAmmoProviderComponentState state)
            return;

        var oldIndex = ent.Comp.CurrentIndex;
        ent.Comp.CurrentIndex = state.CurrentIndex;
        ent.Comp.Chambers = new bool?[state.Chambers.Length];

        // Need to copy across the state rather than the ref.
        for (var i = 0; i < ent.Comp.AmmoSlots.Count; i++)
        {
            ent.Comp.AmmoSlots[i] = EnsureEntity<RevolverAmmoProviderComponent>(state.AmmoSlots[i], ent);
            ent.Comp.Chambers[i] = state.Chambers[i];
        }

        // Handle spins
        if (oldIndex != state.CurrentIndex)
        {
            UpdateAmmoCount(ent, prediction: false);
        }
    }

    public bool TryRevolverInsert(Entity<RevolverAmmoProviderComponent> ent, EntityUid insertEnt, EntityUid? user)
    {
        if (_whitelistSystem.IsWhitelistFail(ent.Comp.Whitelist, insertEnt))
            return false;

        // If it's a speedloader try to get ammo from it.
        if (HasComp<SpeedLoaderComponent>(insertEnt))
        {
            var freeSlots = 0;

            for (var i = 0; i < ent.Comp.Capacity; i++)
            {
                if (ent.Comp.AmmoSlots[i] != null || ent.Comp.Chambers[i] != null)
                    continue;

                freeSlots++;
            }

            if (freeSlots == 0)
            {
                Popup(Loc.GetString("gun-revolver-full"), ent, user);
                return false;
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(insertEnt);
            var ammo = new List<(EntityUid? Entity, IShootable Shootable)>(freeSlots);
            var ev = new TakeAmmoEvent(freeSlots, ammo, xform.Coordinates, user);
            RaiseLocalEvent(insertEnt, ev);

            if (ev.Ammo.Count == 0)
            {
                Popup(Loc.GetString("gun-speedloader-empty"), ent, user);
                return false;
            }

            for (var i = 0; i < ent.Comp.Capacity; i++)
            {
                var index = (ent.Comp.CurrentIndex + i) % ent.Comp.Capacity;

                if (ent.Comp.AmmoSlots[index] != null ||
                    ent.Comp.Chambers[index] != null)
                {
                    continue;
                }

                var ammoEnt = ev.Ammo.Last().Entity;
                ev.Ammo.RemoveAt(ev.Ammo.Count - 1);

                if (ammoEnt == null)
                {
                    Log.Error($"Tried to load hitscan into a revolver which is unsupported");
                    continue;
                }

                ent.Comp.AmmoSlots[index] = ammoEnt.Value;
                Containers.Insert(ammoEnt.Value, ent.Comp.AmmoContainer);
                SetChamber(ent, insertEnt, index);

                if (ev.Ammo.Count == 0)
                    break;
            }

            DebugTools.Assert(ammo.Count == 0);
            UpdateRevolverAppearance(ent);
            UpdateAmmoCount(ent);
            Dirty(ent);

            Audio.PlayPredicted(ent.Comp.SoundInsert, ent, user);
            Popup(Loc.GetString("gun-revolver-insert"), ent, user);
            return true;
        }

        // Try to insert the entity directly.
        for (var i = 0; i < ent.Comp.Capacity; i++)
        {
            var index = (ent.Comp.CurrentIndex + i) % ent.Comp.Capacity;

            if (ent.Comp.AmmoSlots[index] != null ||
                ent.Comp.Chambers[index] != null)
            {
                continue;
            }

            ent.Comp.AmmoSlots[index] = insertEnt;
            Containers.Insert(insertEnt, ent.Comp.AmmoContainer);
            SetChamber(ent, insertEnt, index);
            Audio.PlayPredicted(ent.Comp.SoundInsert, ent, user);
            Popup(Loc.GetString("gun-revolver-insert"), ent, user);
            UpdateRevolverAppearance(ent);
            UpdateAmmoCount(ent);
            Dirty(ent);
            return true;
        }

        Popup(Loc.GetString("gun-revolver-full"), ent, user);
        return false;
    }

    private void SetChamber(Entity<RevolverAmmoProviderComponent> ent, Entity<CartridgeAmmoComponent?> ammo, int index)
    {
        if (!Resolve(ammo, ref ammo.Comp, false) || ammo.Comp.Spent)
        {
            ent.Comp.Chambers[index] = false;
            return;
        }

        ent.Comp.Chambers[index] = true;
    }

    private void OnRevolverVerbs(EntityUid uid, RevolverAmmoProviderComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("gun-revolver-empty"),
            Disabled = !AnyRevolverCartridges(component),
            Act = () => EmptyRevolver((uid, component), args.User),
            Priority = 1
        });

        args.Verbs.Add(new AlternativeVerb()
        {
            Text = Loc.GetString("gun-revolver-spin"),
            // Category = VerbCategory.G,
            Act = () => SpinRevolver((uid, component), args.User)
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

    public void EmptyRevolver(Entity<RevolverAmmoProviderComponent> ent, EntityUid? user = null)
    {
        var mapCoordinates = TransformSystem.GetMapCoordinates(ent);
        var anyEmpty = false;

        for (var i = 0; i < ent.Comp.Capacity; i++)
        {
            var chamber = ent.Comp.Chambers[i];
            var slot = ent.Comp.AmmoSlots[i];

            if (slot == null)
            {
                if (chamber == null)
                    continue;

                // Too lazy to make a new method don't sue me.
                if (!_netManager.IsClient)
                {
                    var uid = Spawn(ent.Comp.FillPrototype, mapCoordinates);

                    if (TryComp<CartridgeAmmoComponent>(uid, out var cartridge))
                        SetCartridgeSpent(uid, cartridge, !(bool)chamber);

                    EjectCartridge(uid);
                }

                ent.Comp.Chambers[i] = null;
                anyEmpty = true;
            }
            else
            {
                ent.Comp.AmmoSlots[i] = null;
                Containers.Remove(slot.Value, ent.Comp.AmmoContainer);
                ent.Comp.Chambers[i] = null;

                if (!_netManager.IsClient)
                    EjectCartridge(slot.Value);

                anyEmpty = true;
            }
        }

        if (anyEmpty)
        {
            Audio.PlayPredicted(ent.Comp.SoundEject, ent, user);
            UpdateAmmoCount(ent, prediction: false);
            UpdateRevolverAppearance(ent);
            Dirty(ent);
        }
    }

    private void UpdateRevolverAppearance(Entity<RevolverAmmoProviderComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        var count = GetRevolverCount(ent.Comp);
        Appearance.SetData(ent, AmmoVisuals.HasAmmo, count != 0, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoCount, count, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.Capacity, appearance);
    }

    protected virtual void SpinRevolver(Entity<RevolverAmmoProviderComponent> ent, EntityUid? user = null)
    {
        Audio.PlayPredicted(ent.Comp.SoundSpin, ent, user);
        Popup(Loc.GetString("gun-revolver-spun"), ent, user);
    }

    private void OnRevolverTakeAmmo(Entity<RevolverAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var currentIndex = ent.Comp.CurrentIndex;
        Cycle(ent.Comp, args.Shots);

        // Revolvers provide the bullets themselves rather than the cartridges so they stay in the revolver.
        for (var i = 0; i < args.Shots; i++)
        {
            var index = (currentIndex + i) % ent.Comp.Capacity;
            var chamber = ent.Comp.Chambers[index];
            EntityUid? ammoEnt = null;

            // Get contained entity if it exists.
            if (ent.Comp.AmmoSlots[index] != null)
            {
                ammoEnt = ent.Comp.AmmoSlots[index]!;
                ent.Comp.Chambers[index] = false;
            }
            // Try to spawn a round if it's available.
            else if (chamber != null)
            {
                if (chamber == true)
                {
                    // Pretend it's always been there.
                    ammoEnt = Spawn(ent.Comp.FillPrototype, args.Coordinates);

                    if (!_netManager.IsClient)
                    {
                        ent.Comp.AmmoSlots[index] = ammoEnt;
                        Containers.Insert(ammoEnt.Value, ent.Comp.AmmoContainer);
                    }

                    ent.Comp.Chambers[index] = false;
                }
            }

            // Chamber empty or spent
            if (ammoEnt == null)
                continue;

            if (TryComp<CartridgeAmmoComponent>(ammoEnt, out var cartridge))
            {
                if (cartridge.Spent)
                    continue;

                // Mark cartridge as spent and if it's caseless delete from the chamber slot.
                SetCartridgeSpent(ammoEnt.Value, cartridge, true);
                var spawned = Spawn(cartridge.Prototype, args.Coordinates);
                args.Ammo.Add((spawned, EnsureComp<AmmoComponent>(spawned)));

                if (cartridge.DeleteOnSpawn)
                {
                    ent.Comp.AmmoSlots[index] = null;
                    ent.Comp.Chambers[index] = null;
                }
            }
            else
            {
                ent.Comp.AmmoSlots[index] = null;
                ent.Comp.Chambers[index] = null;
                args.Ammo.Add((ammoEnt.Value, EnsureComp<AmmoComponent>(ammoEnt.Value)));
            }

            // Delete the cartridge entity on client
            if (_netManager.IsClient)
            {
                QueueDel(ammoEnt);
            }
        }

        UpdateAmmoCount(ent, prediction: false);
        UpdateRevolverAppearance(ent);
        Dirty(ent);
    }

    private void Cycle(RevolverAmmoProviderComponent component, int count = 1)
    {
        component.CurrentIndex = (component.CurrentIndex + count) % component.Capacity;
    }

    private void OnRevolverInit(Entity<RevolverAmmoProviderComponent> ent, ref ComponentInit args)
    {
        ent.Comp.AmmoContainer = Containers.EnsureContainer<Container>(ent, RevolverContainer);
        ent.Comp.AmmoSlots.EnsureCapacity(ent.Comp.Capacity);
        var remainder = ent.Comp.Capacity - ent.Comp.AmmoSlots.Count;

        for (var i = 0; i < remainder; i++)
        {
            ent.Comp.AmmoSlots.Add(null);
        }

        ent.Comp.Chambers = new bool?[ent.Comp.Capacity];

        if (ent.Comp.FillPrototype != null)
        {
            for (var i = 0; i < ent.Comp.Capacity; i++)
            {
                if (ent.Comp.AmmoSlots[i] != null)
                {
                    ent.Comp.Chambers[i] = null;
                    continue;
                }

                ent.Comp.Chambers[i] = true;
            }
        }

        DebugTools.Assert(ent.Comp.AmmoSlots.Count == ent.Comp.Capacity);
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
