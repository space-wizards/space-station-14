using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared.Weapons.Ranged.Systems;

// Randomizes shots-per-burst each time a burst completes for guns with GunBurstVarianceComponent.
public sealed class GunBurstVarianceSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GunBurstVarianceComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<GunBurstVarianceComponent, GunRefreshModifiersEvent>(OnRefreshModifiers);
        SubscribeLocalEvent<GunBurstVarianceComponent, GunShotEvent>(OnGunShot);
    }

    private void OnMapInit(Entity<GunBurstVarianceComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        ent.Comp.CurrentShots = _random.Next(ent.Comp.MinShots, ent.Comp.MaxShots + 1);
        Dirty(ent, ent.Comp);

        if (HasComp<GunComponent>(ent.Owner))
            _gun.RefreshModifiers(ent.Owner);
    }

    private void OnRefreshModifiers(Entity<GunBurstVarianceComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (ent.Comp.CurrentShots <= 0)
            return;

        args.ShotsPerBurst = ent.Comp.CurrentShots;
    }

    private void OnGunShot(Entity<GunBurstVarianceComponent> ent, ref GunShotEvent args)
    {
        if (!TryComp<GunComponent>(ent.Owner, out var gun))
            return;

        if (gun.SelectedMode != SelectiveFire.Burst || gun.BurstActivated)
            return;

        // Burst just ended — re-roll for the next one.
        if (_net.IsClient)
            return;

        ent.Comp.CurrentShots = _random.Next(ent.Comp.MinShots, ent.Comp.MaxShots + 1);
        Dirty(ent, ent.Comp);
        _gun.RefreshModifiers(ent.Owner);
    }
}
