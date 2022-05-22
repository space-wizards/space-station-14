using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared.Weapons.Ranged;

public partial class SharedNewGunSystem
{
    private void InitializeRevolver()
    {
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, ComponentInit>(OnRevolverInit);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, ComponentGetState>(OnRevolverGetState);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, ComponentHandleState>(OnRevolverHandleState);
        SubscribeLocalEvent<SharedRevolverAmmoProviderComponent, TakeAmmoEvent>(OnRevolverTakeAmmo);
    }

    private void OnRevolverHandleState(EntityUid uid, SharedRevolverAmmoProviderComponent component, ref ComponentHandleState args)
    {
        if (args.Current is not RevolverAmmoProviderComponentState state) return;

        component.CurrentIndex = state.CurrentIndex;
        component.AmmoSlots = state.AmmoSlots;
        component.Chambers = state.Chambers;
    }

    private void OnRevolverGetState(EntityUid uid, SharedRevolverAmmoProviderComponent component, ref ComponentGetState args)
    {
        args.State = new RevolverAmmoProviderComponentState
        {
            CurrentIndex = component.CurrentIndex,
            AmmoSlots = component.AmmoSlots,
            Chambers = component.Chambers,
        };
    }

    private void OnRevolverTakeAmmo(EntityUid uid, SharedRevolverAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var currentIndex = component.CurrentIndex;
        Cycle(component, args.Shots);

        for (var i = 0; i < args.Shots; i++)
        {
            var index = (currentIndex + i) % component.Capacity;

            // Get unspawned ent first if possible.
            if (component.Chambers[index] != null)
            {
                if (component.Chambers[index] == true)
                {
                    var ent = Spawn(component.FillPrototype, args.Coordinates);
                    component.Chambers[index] = false;
                    args.Ammo.Add(ent);
                }
                else
                {
                    args.Shots -= 1;
                }
            }
            else if (component.AmmoSlots[index] != null)
            {
                var ent = component.AmmoSlots[index]!;
                component.AmmoContainer.Remove(ent.Value);
                component.AmmoSlots[index] = null;
                args.Ammo.Add(ent.Value);
                Transform(ent.Value).Coordinates = args.Coordinates;
            }
            else
            {
                args.Shots -= 1;
            }
        }

        Dirty(component);
    }

    private void Cycle(SharedRevolverAmmoProviderComponent component, int count = 1)
    {
        component.CurrentIndex = (component.CurrentIndex + count) % component.Capacity;
    }

    private void OnRevolverInit(EntityUid uid, SharedRevolverAmmoProviderComponent component, ComponentInit args)
    {
        component.AmmoContainer = Containers.EnsureContainer<Container>(uid, "revolver-ammo-container");
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
    private sealed class RevolverAmmoProviderComponentState : ComponentState
    {
        public int CurrentIndex;
        public EntityUid?[] AmmoSlots = default!;
        public bool?[] Chambers = default!;
    }
}
