using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Server.Mech.Equipment.EntitySystems;
public sealed class MechGunSystem
{
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly GunSystem _gunSys = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechGunComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<MechGunComponent, InteractNoHandEvent>(OnInteract);
    }

    private void OnStartup(EntityUid uid, MechGunComponent component, ComponentStartu args)
    {
        component.GunContainer = _container.EnsureContainer<Container>(uid, "item-container");

        if (!PrototypeManager.TryIndex<EntityPrototype>(component.GunPrototype, out var prototype))
            return;

        var entity = _entityManager.SpawnEntity(component.GunPrototype, Transform(uid).Coordinates);
        component.GunContainer.Insert(entity);
    }

    private void OnInteract(EntityUid uid, MechGunComponent component, InteractNoHandEvent args)
    {
        if (!component.GunContainer._containerList.Any())
            return;

        var entity = component.GunContainer._containerList.First(); // Should only be one element

        if (!TryComp<GunComponent>(entity, out var gunComp))
            return;

        _gunSys.Shoot(entity, gunComp, new List<EntityUid>());
    }
}
