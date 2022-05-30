using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    protected virtual void InitializeRevolver()
    {
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentGetState>(OnRevolverGetState);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentHandleState>(OnRevolverHandleState);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, ComponentInit>(OnRevolverInit);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, TakeAmmoEvent>(OnRevolverTakeAmmo);
        SubscribeLocalEvent<RevolverAmmoProviderComponent, GetVerbsEvent<Verb>>(OnRevolverVerbs);
    }

    private void OnRevolverGetState(EntityUid uid, RevolverAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new RevolverAmmoProviderComponentState
        {
            CurrentIndex = component.CurrentIndex,
            AmmoSlots = component.AmmoSlots,
            Chambers = component.Chambers,
        };
    }

    private void OnRevolverHandleState(EntityUid uid, RevolverAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RevolverAmmoProviderComponentState state) return;

        var oldIndex = component.CurrentIndex;
        component.CurrentIndex = state.CurrentIndex;

        component.AmmoSlots = new EntityUid?[state.AmmoSlots.Length];
        component.Chambers = new bool?[state.Chambers.Length];

        DebugTools.Assert(component.AmmoSlots.Length == component.Chambers.Length);

        // Need to copy across the state rather than the ref.
        for (var i = 0; i < component.AmmoSlots.Length; i++)
        {
            component.AmmoSlots[i] = state.AmmoSlots[i];
            component.Chambers[i] = state.Chambers[i];
        }

        // Handle spins
        if (Timing.IsFirstTimePredicted)
        {
            if (oldIndex != state.CurrentIndex)
                UpdateAmmoCount(uid);
        }
    }

    private void OnRevolverVerbs(EntityUid uid, RevolverAmmoProviderComponent component, GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract) return;

        args.Verbs.Add(new Verb()
        {
            Text = "Spin revolver",
            // Category = VerbCategory.G,
            Act = () => SpinRevolver(component, args.User)
        });
    }

    protected abstract void SpinRevolver(RevolverAmmoProviderComponent component, EntityUid? user = null);

    private void OnRevolverTakeAmmo(EntityUid uid, RevolverAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var currentIndex = component.CurrentIndex;
        Cycle(component, args.Shots);

        for (var i = 0; i < args.Shots; i++)
        {
            var index = (currentIndex + i) % component.Capacity;
            var chamber = component.Chambers[index];

            // Get unspawned ent first if possible.
            if (chamber != null)
            {
                if (chamber == true)
                {
                    var ent = Spawn(component.FillPrototype, args.Coordinates);
                    component.Chambers[index] = false;
                    args.Ammo.Add(EnsureComp<AmmoComponent>(ent));
                }
            }
            else if (component.AmmoSlots[index] != null)
            {
                var ent = component.AmmoSlots[index]!;
                component.AmmoContainer.Remove(ent.Value);
                component.AmmoSlots[index] = null;
                args.Ammo.Add(EnsureComp<AmmoComponent>(ent.Value));
                Transform(ent.Value).Coordinates = args.Coordinates;
            }
        }

        Dirty(component);
    }

    private void Cycle(RevolverAmmoProviderComponent component, int count = 1)
    {
        component.CurrentIndex = (component.CurrentIndex + count) % component.Capacity;
    }

    private void OnRevolverInit(EntityUid uid, RevolverAmmoProviderComponent component, ComponentInit args)
    {
        component.AmmoContainer = Containers.EnsureContainer<Container>(uid, "revolver-ammo");
        component.AmmoSlots = new EntityUid?[component.Capacity];
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
    }

    [Serializable, NetSerializable]
    protected sealed class RevolverAmmoProviderComponentState : ComponentState
    {
        public int CurrentIndex;
        public EntityUid?[] AmmoSlots = default!;
        public bool?[] Chambers = default!;
    }

    public sealed class RevolverSpinEvent : EntityEventArgs
    {

    }
}
