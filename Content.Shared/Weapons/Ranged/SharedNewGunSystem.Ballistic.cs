using Robust.Shared.Containers;

namespace Content.Shared.Weapons.Ranged;

public abstract partial class SharedNewGunSystem
{
    private void InitializeBallistic()
    {
        SubscribeLocalEvent<BallisticAmmoProviderComponent, ComponentInit>(OnBallisticInit);
        SubscribeLocalEvent<BallisticAmmoProviderComponent, TakeAmmoEvent>(OnBallisticTakeAmmo);
    }

    private void OnBallisticInit(EntityUid uid, BallisticAmmoProviderComponent component, ComponentInit args)
    {
        component.Container = Containers.EnsureContainer<Container>(uid, "ballistic-ammo");
        component.UnspawnedCount = component.Capacity;

        if (component.FillProto != null)
        {
            component.UnspawnedCount -= Math.Min(component.UnspawnedCount, component.Container.ContainedEntities.Count);
        }
        else
        {
            component.UnspawnedCount = 0;
        }
    }

    private void OnBallisticTakeAmmo(EntityUid uid, BallisticAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var shots = 0;

        if (component.Cycled)
        {
            for (var i = 0; i < args.Shots; i--)
            {
                if (component.Entities.TryPop(out var existing))
                {
                    component.Container.Remove(existing);
                    args.Ammo.Add(EnsureComp<NewAmmoComponent>(existing));
                    shots++;
                }
                else if (component.UnspawnedCount > 0)
                {
                    component.UnspawnedCount--;
                    var ent = Spawn(component.FillProto, args.Coordinates);
                    args.Ammo.Add(EnsureComp<NewAmmoComponent>(ent));
                    shots++;
                }

                if (!component.AutoCycle)
                {
                    component.Cycled = false;
                    break;
                }
            }
        }

        args.Shots = shots;
    }
}
