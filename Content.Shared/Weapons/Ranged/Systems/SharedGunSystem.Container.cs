using System.Linq;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    private void InitializeContainer()
    {
        SubscribeLocalEvent<ContainerAmmoProviderComponent, TakeAmmoEvent>(OnContainerTakeAmmo);
        SubscribeLocalEvent<ContainerAmmoProviderComponent, GetAmmoCountEvent>(OnContainerAmmoCount);
    }

    private void OnContainerTakeAmmo(Entity<ContainerAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        ent.Comp.ProviderUid ??= ent;
        if (!Containers.TryGetContainer(ent.Comp.ProviderUid.Value, ent.Comp.Container, out var container))
            return;

        for (var i = 0; i < args.Shots; i++)
        {
            if (!container.ContainedEntities.Any())
                break;

            var ammoEnt = container.ContainedEntities[0];

            if (_netManager.IsServer)
                Containers.Remove(ammoEnt, container);

            args.Ammo.Add((ammoEnt, EnsureShootable(ammoEnt)));
        }
    }

    private void OnContainerAmmoCount(Entity<ContainerAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        ent.Comp.ProviderUid ??= ent;
        if (!Containers.TryGetContainer(ent.Comp.ProviderUid.Value, ent.Comp.Container, out var container))
        {
            args.Capacity = 0;
            args.Count = 0;
            return;
        }

        args.Capacity = int.MaxValue;
        args.Count = container.ContainedEntities.Count;
    }
}
