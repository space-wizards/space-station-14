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

    private void OnSolutionTakeAmmo(EntityUid uid, SolutionAmmoProviderComponent component, TakeAmmoEvent args)
    {
        var shots = Math.Min(args.Shots, component.Shots);

        // Don't dirty if it's an empty fire.
        if (shots == 0)
            return;

        for (var i = 0; i < shots; i++)
        {
            args.Ammo.Add(GetSolutionShot(uid, component, args.Coordinates));
            component.Shots--;
        }

        UpdateSolutionShots(uid, component);
        UpdateSolutionAppearance(uid, component);
    }

    private void OnSolutionAmmoCount(EntityUid uid, SolutionAmmoProviderComponent component, ref GetAmmoCountEvent args)
    {
        args.Count = component.Shots;
        args.Capacity = component.MaxShots;
    }

    protected virtual void UpdateSolutionShots(EntityUid uid, SolutionAmmoProviderComponent component, Solution? solution = null)
    {

    }

    protected virtual (EntityUid Entity, IShootable) GetSolutionShot(EntityUid uid, SolutionAmmoProviderComponent component, EntityCoordinates position)
    {
        var ent = Spawn(component.Prototype, position);
        return (ent, EnsureComp<AmmoComponent>(ent));
    }

    protected void UpdateSolutionAppearance(EntityUid uid, SolutionAmmoProviderComponent component)
    {
        if (!TryComp<AppearanceComponent>(uid, out var appearance))
            return;

        Appearance.SetData(uid, AmmoVisuals.HasAmmo, component.Shots != 0, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoCount, component.Shots, appearance);
        Appearance.SetData(uid, AmmoVisuals.AmmoMax, component.MaxShots, appearance);
    }
}
