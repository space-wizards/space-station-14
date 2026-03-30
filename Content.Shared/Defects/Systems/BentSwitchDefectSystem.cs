using Content.Shared.Defects.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared.Defects.Systems;

/// <summary>
/// At MapInit, forces any gun with FullAuto in its available modes into
/// semi-automatic only via SharedGunSystem.SetAvailableModes.
/// Does nothing on guns that don't have FullAuto.
/// </summary>
public sealed class BentSwitchDefectSystem : EntitySystem
{
    [Dependency] private readonly SharedGunSystem _gunSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BentSwitchDefectComponent, MapInitEvent>(OnMapInit,
            after: new[] { typeof(DefectSystem) });
    }

    private void OnMapInit(Entity<BentSwitchDefectComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<GunComponent>(ent.Owner, out var gun))
            return;

        if ((gun.AvailableModes & SelectiveFire.FullAuto) == 0)
            return;

        _gunSystem.SetAvailableModes((ent.Owner, gun), SelectiveFire.SemiAuto);
    }
}
