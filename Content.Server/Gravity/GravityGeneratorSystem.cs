using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.EntitySystems;
using Content.Shared.Explosion.Components;
using Content.Shared.Gravity;
using Content.Shared.Power.Components;

namespace Content.Server.Gravity;

public sealed class GravityGeneratorSystem : SharedGravityGeneratorSystem
{
    [Dependency] private readonly GravitySystem _gravitySystem = default!;
    [Dependency] private readonly SharedPointLightSystem _lights = default!;
    [Dependency] private readonly ExplosionSystem _explosion = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
        SubscribeLocalEvent<PowerChargeComponent, ChangedMachineBeforeBreakageEvent>(OnBeforeBreak);

    }

    private void OnBeforeBreak(EntityUid uid, PowerChargeComponent component, ChangedMachineBeforeBreakageEvent _)
    {
        if (TryComp<ExplosiveComponent>(uid, out var explosiveComponent) && component.Charge > 0)
        {
            _explosion.TriggerExplosive(uid,
                explosiveComponent,
                false,
                // min of half intensity to prevent very small explosions
                explosiveComponent.TotalIntensity * (component.Charge / 2f + .5f));
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        var query = EntityQueryEnumerator<GravityGeneratorComponent, PowerChargeComponent>();
        while (query.MoveNext(out var uid, out var grav, out var charge))
        {
            if (!_lights.TryGetLight(uid, out var pointLight))
                continue;

            _lights.SetEnabled(uid, charge.Charge > 0, pointLight);
            _lights.SetRadius(uid, MathHelper.Lerp(grav.LightRadiusMin, grav.LightRadiusMax, charge.Charge),
                pointLight);
        }
    }

    private void OnActivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineActivatedEvent args)
    {
        ent.Comp.GravityActive = true;
        Dirty(ent, ent.Comp);

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.EnableGravity(xform.ParentUid, gravity);
        }
    }

    private void OnDeactivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        ent.Comp.GravityActive = false;
        Dirty(ent, ent.Comp);

        var xform = Transform(ent);

        if (TryComp(xform.ParentUid, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
        }
    }

    private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
    {
        if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
        }
    }
}
