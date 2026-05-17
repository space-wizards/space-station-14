using Content.Shared.Chemistry.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Shared.Map;

namespace Content.Shared.Weapons.Ranged.Systems;

public partial class SharedGunSystem
{
    protected virtual void InitializeSolution()
    {
        SubscribeLocalEvent<SolutionAmmoProviderComponent, TakeAmmoEvent>(OnSolutionTakeAmmo);
        SubscribeLocalEvent<SolutionAmmoProviderComponent, GetAmmoCountEvent>(OnSolutionAmmoCount);
    }

    private void OnSolutionTakeAmmo(Entity<SolutionAmmoProviderComponent> ent, ref TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, ent.Comp.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0)
            return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetSolutionShot(ent, args.Coordinates));
            ent.Comp.Shots--;
        }

        UpdateSolutionShots(ent);
        UpdateSolutionAppearance(ent);
    }

    private void OnSolutionAmmoCount(Entity<SolutionAmmoProviderComponent> ent, ref GetAmmoCountEvent args)
    {
        args.Count = ent.Comp.Shots;
        args.Capacity = ent.Comp.MaxShots;
    }

    protected virtual void UpdateSolutionShots(Entity<SolutionAmmoProviderComponent> ent, Solution? solution = null) { }

    protected virtual (EntityUid Entity, IShootable) GetSolutionShot(Entity<SolutionAmmoProviderComponent> ent, EntityCoordinates position)
    {
        var shot = Spawn(ent.Comp.Prototype, position);
        return (shot, EnsureShootable(shot));
    }

    protected void UpdateSolutionAppearance(Entity<SolutionAmmoProviderComponent> ent)
    {
        if (!TryComp<AppearanceComponent>(ent, out var appearance))
            return;

        Appearance.SetData(ent, AmmoVisuals.HasAmmo, ent.Comp.Shots != 0, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoCount, ent.Comp.Shots, appearance);
        Appearance.SetData(ent, AmmoVisuals.AmmoMax, ent.Comp.MaxShots, appearance);
    }
}
