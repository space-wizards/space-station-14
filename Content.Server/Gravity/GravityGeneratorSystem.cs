using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Shared.Gravity;

namespace Content.Server.Gravity;

public sealed partial class GravityGeneratorSystem : SharedGravityGeneratorSystem
{
    [Dependency] private GravitySystem _gravitySystem = default!;
    [Dependency] private SharedPointLightSystem _lights = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GravityGeneratorComponent, EntParentChangedMessage>(OnParentChanged);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineActivatedEvent>(OnActivated);
        SubscribeLocalEvent<GravityGeneratorComponent, ChargedMachineDeactivatedEvent>(OnDeactivated);
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
        SetActive((ent.Owner, ent.Comp), true);
    }

    private void OnDeactivated(Entity<GravityGeneratorComponent> ent, ref ChargedMachineDeactivatedEvent args)
    {
        SetActive((ent.Owner, ent.Comp), false);
    }

    public void SetActive(Entity<GravityGeneratorComponent?> ent, bool active)
    {
        if (!Resolve(ent, ref ent.Comp) || ent.Comp.GravityActive == active)
            return;

        ent.Comp.GravityActive = active;
        Dirty(ent);
        var xform = Transform(ent);
        if (!TryComp<GravityComponent>(xform.ParentUid, out var gravity))
            return;

        if (active)
            _gravitySystem.EnableGravity(xform.ParentUid, gravity);
        else
            _gravitySystem.RefreshGravity(xform.ParentUid, gravity);
    }

    private void OnParentChanged(EntityUid uid, GravityGeneratorComponent component, ref EntParentChangedMessage args)
    {
        if (component.GravityActive && TryComp(args.OldParent, out GravityComponent? gravity))
        {
            _gravitySystem.RefreshGravity(args.OldParent.Value, gravity);
        }
    }
}
