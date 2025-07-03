using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Timing;

namespace Content.Client.Movement.Systems;

public sealed class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, AppearanceChangeEvent>(OnJetpackAppearance);
    }

    // No predicted atmos so you'd have to do a lot of funny to get this working.
    protected override bool CanEnable(Entity<JetpackComponent> jetpack)
        => false;


    private void OnJetpackAppearance(EntityUid uid, JetpackComponent component, ref AppearanceChangeEvent args)
    {
        Appearance.TryGetData<bool>(uid, JetpackVisuals.Enabled, out var enabled, args.Component);

        if (TryComp<ClothingComponent>(uid, out var clothing))
            _clothing.SetEquippedPrefix(uid, enabled ? "on" : null, clothing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        // TODO: Please don't copy-paste this I beg
        // make a generic particle emitter system / actual particles instead.
        var query = EntityQueryEnumerator<ActiveJetpackComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            var transform = Transform(uid);

            // As long as the component is clientside it bulldozes *EVERYTHING*.
            var currentCoords = _transform.GetMoverCoordinates(transform.Coordinates);
            if (comp.LastCoordinates is not { } lastCoords)
            {
                comp.LastCoordinates = currentCoords;
                continue;
            }
            else if (_transform.InRange(transform.Coordinates, lastCoords, comp.MaxDistance) && _timing.CurTime < comp.TargetTime)
                continue;

            comp.LastCoordinates = currentCoords;
            comp.TargetTime = _timing.CurTime + TimeSpan.FromSeconds(comp.EffectCooldown);
            CreateParticles(uid, transform);
        }
    }

    private void CreateParticles(EntityUid uid, TransformComponent uidXform)
    {
        // Don't show particles unless the user is moving.
        if (Container.TryGetContainingContainer((uid, uidXform, null), out var container) &&
            TryComp<PhysicsComponent>(container.Owner, out var body) &&
            body.LinearVelocity.LengthSquared() < 1f)
        {
            return;
        }

        var coordinates = uidXform.Coordinates;
        var gridUid = _transform.GetGrid(coordinates);

        if (TryComp<MapGridComponent>(gridUid, out var grid))
        {
            coordinates = new EntityCoordinates(gridUid.Value, _mapSystem.WorldToLocal(gridUid.Value, grid, _transform.ToMapCoordinates(coordinates).Position));
        }
        else if (uidXform.MapUid != null)
        {
            coordinates = new EntityCoordinates(uidXform.MapUid.Value, _transform.GetWorldPosition(uidXform));
        }
        else
            return;

        Spawn("JetpackEffect", coordinates);
    }
}
