using System.Numerics;
using Content.Server.Mech.Equipment.Components;
using Content.Server.Weapons.Ranged.Systems;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Equipment.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Mech.Equipment.EntitySystems;
public sealed class MechGunSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MechEquipmentComponent, GunShotEvent>(MechGunShot);
    }

    private void MechGunShot(EntityUid uid, MechEquipmentComponent component, ref GunShotEvent args)
    {
        if (!component.EquipmentOwner.HasValue)
            return;

        if (!TryComp<MechComponent>(component.EquipmentOwner.Value, out var mech))
            return;

        var equipment = new List<EntityUid>(mech.EquipmentContainer.ContainedEntities);
        foreach (var ent in equipment)
        {
            if (HasComp<AmmoComponent>(ent))
            {
                mech.EquipmentContainer.Remove(ent);
                _throwing.TryThrow(ent, _random.NextVector2(), _random.Next(5));
            }
        }
    }
}
