using Content.Shared.Interaction;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Weapons.Misc;

public abstract class SharedTetherGunSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TetherGunComponent, ActivateInWorldEvent>(OnTetherActivate);
        SubscribeLocalEvent<TetherGunComponent, AfterInteractEvent>(OnTetherRanged);
    }

    private void OnTetherRanged(EntityUid uid, TetherGunComponent component, AfterInteractEvent args)
    {
        if (args.Target == null)
            return;

        TryTether(uid, args.Target.Value, component);
    }

    private void OnTetherActivate(EntityUid uid, TetherGunComponent component, ActivateInWorldEvent args)
    {
        StopTether(component);
    }

    public void TryTether(EntityUid gun, EntityUid target, TetherGunComponent? component = null)
    {
        if (!Resolve(gun, ref component))
            return;

        if (!CanTether(component, target))
            return;

        StartTether(gun, component, target);
    }

    private bool CanTether(TetherGunComponent component, EntityUid target)
    {
        if (HasComp<TetheredComponent>(target) || !TryComp<PhysicsComponent>(target, out var physics))
            return false;

        if (physics.BodyType == BodyType.Static && !component.CanUnanchor)
            return false;

        if (physics.Mass > component.MassLimit)
            return false;

        return true;
    }

    private void StartTether(EntityUid gunUid, TetherGunComponent component, EntityUid target)
    {
        if (component.Tethered != null)
        {
            StopTether(component);
        }

        component.Tethered = target;
        var tethered = EnsureComp<TetheredComponent>(target);
        tethered.Tetherer = gunUid;
        Dirty(tethered);
        Dirty(component);
    }

    private void StopTether(TetherGunComponent component)
    {
        if (component.Tethered == null)
            return;

        RemCompDeferred<TetheredComponent>(component.Tethered.Value);
        component.Tethered = null;
        Dirty(component);
    }
}
