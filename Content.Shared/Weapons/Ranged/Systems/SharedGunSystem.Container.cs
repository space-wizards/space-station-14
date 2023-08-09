using System.Linq;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private void InitializeContainer()
    {
        SubscribeLocalEvent<ContainerAmmoProviderComponent, TakeAmmoEvent>(OnContainerTakeAmmo);
        SubscribeLocalEvent<ContainerAmmoProviderComponent, GetAmmoCountEvent>(OnContainerAmmoCount);
    }

    private void OnContainerTakeAmmo(EntityUid uid, ContainerAmmoProviderComponent component, TakeAmmoEvent args)
    {
        component.ProviderUid ??= uid;
        if (!_container.TryGetContainer(component.ProviderUid.Value, component.Container, out var container))
            return;

        for (var i = 0; i < args.Shots; i++)
        {
            if (!container.ContainedEntities.Any())
                break;

            var ent = container.ContainedEntities[0];

            if (_netMan.IsServer)
                container.Remove(ent);

            args.Ammo.Add((ent, EnsureComp<AmmoComponent>(ent)));
        }
    }

    private void OnContainerAmmoCount(EntityUid uid, ContainerAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        component.ProviderUid ??= uid;
        if (!_container.TryGetContainer(component.ProviderUid.Value, component.Container, out var container))
        {
            args.Capacity = 0;
            args.Count = 0;
            return;
        }

        args.Capacity = int.MaxValue;
        args.Count = container.ContainedEntities.Count;
    }
}
