using Content.Shared.Light.Components;
using Content.Shared.SurveillanceCamera;
using Content.Shared.SurveillanceCamera.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;

namespace Content.Shared.Light.EntitySystems;

public sealed class CameraLightCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedSurveillanceCameraSystem _camera = default!;

    private EntityQuery<CameraLightOnCollideComponent> _lightQuery;

    public override void Initialize()
    {
        base.Initialize();

        _lightQuery = GetEntityQuery<CameraLightOnCollideComponent>();

        SubscribeLocalEvent<CameraLightOnCollideColliderComponent, PreventCollideEvent>(OnPreventCollide);
        SubscribeLocalEvent<CameraLightOnCollideColliderComponent, StartCollideEvent>(OnStart);
        SubscribeLocalEvent<CameraLightOnCollideColliderComponent, EndCollideEvent>(OnEnd);

        SubscribeLocalEvent<CameraLightOnCollideColliderComponent, ComponentShutdown>(OnCollideShutdown);
    }

    private void OnCollideShutdown(Entity<CameraLightOnCollideColliderComponent> ent, ref ComponentShutdown args)
    {
        // TODO: Check this on the event.
        if (TerminatingOrDeleted(ent.Owner))
            return;

        // Regenerate contacts for everything we were colliding with.
        var contacts = _physics.GetContacts(ent.Owner);

        while (contacts.MoveNext(out var contact))
        {
            if (!contact.IsTouching)
                continue;

            var other = contact.OtherEnt(ent.Owner);

            if (_lightQuery.HasComp(other))
            {
                _physics.RegenerateContacts(other);
            }
        }
    }

    // You may be wondering what de fok this is doing here.
    // At the moment there's no easy way to do collision whitelists based on components.
    private void OnPreventCollide(Entity<CameraLightOnCollideColliderComponent> ent, ref PreventCollideEvent args)
    {
        if (!_lightQuery.HasComp(args.OtherEntity))
        {
            args.Cancelled = true;
        }
    }

    private void OnEnd(Entity<CameraLightOnCollideColliderComponent> ent, ref EndCollideEvent args)
    {
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        if (!_lightQuery.TryComp(args.OtherEntity, out var light))
            return;

        // TODO: Engine bug IsTouching box2d yay.
        var contacts = _physics.GetTouchingContacts(args.OtherEntity) - 1;

        if (contacts > 0)
            return;

        light.Enabled = false;
        Dirty(args.OtherEntity, light);
        _camera.UpdateVisuals(args.OtherEntity);
    }

    private void OnStart(Entity<CameraLightOnCollideColliderComponent> ent, ref StartCollideEvent args)
    {
        Log.Debug("Checking fixture");
        if (args.OurFixtureId != ent.Comp.FixtureId)
            return;

        Log.Debug("Checking component");
        if (!_lightQuery.TryComp(args.OtherEntity, out var light))
            return;

        Log.Debug("Enabling");
        light.Enabled = true;
        Dirty(args.OtherEntity, light);
        _camera.UpdateVisuals(args.OtherEntity);
    }
}
