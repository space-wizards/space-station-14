using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Ghost.Components;
using Robust.Shared.Audio;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Icarus;

public sealed class IcarusBeamSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQuery<IcarusBeamComponent, TransformComponent>(true);
        foreach (var (beam, trans) in query)
        {
            AccumulateLifetime(beam, frameTime);
            DestroyEntities(beam, trans);
            BurnEntities(beam, trans);

            if (!beam.DestroyTiles)
                continue;

            DestroyTiles(beam, trans);
        }
    }

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<IcarusBeamComponent, ComponentInit>(OnComponentInit);
    }

    private void OnComponentInit(EntityUid uid, IcarusBeamComponent component, ComponentInit args)
    {
        if (TryComp(uid, out PhysicsComponent? phys))
        {
            phys.LinearDamping = 0f;
            phys.Friction = 0f;
            phys.BodyStatus = BodyStatus.InAir;
        }
    }

    public void LaunchInDirection(EntityUid uid, Vector2 dir, IcarusBeamComponent? beam = null)
    {
        if (!Resolve(uid, ref beam))
            return;

        if (TryComp(beam.Owner, out PhysicsComponent? phys))
            phys.ApplyLinearImpulse(dir * beam.Speed);
    }

    private void AccumulateLifetime(IcarusBeamComponent beam, float frameTime)
    {
        beam.Accumulator += frameTime;
        if (beam.Accumulator > beam.Lifetime.TotalSeconds)
        {
            QueueDel(beam.Owner);
        }
    }

    /// <summary>
    /// Destroy any grid tiles in beam radius.
    /// </summary>
    private void DestroyTiles(IcarusBeamComponent component, TransformComponent trans)
    {
        var radius = component.DestroyRadius;
        var worldPos = trans.WorldPosition;

        var circle = new Circle(worldPos, radius);
        var box = new Box2(worldPos - radius, worldPos + radius);

        foreach (var grid in _map.FindGridsIntersecting(trans.MapID, box))
        {
            // Bundle these together so we can use the faster helper to set tiles.
            var toDestroy = new List<(Vector2i, Tile)>();

            foreach (var tile in grid.GetTilesIntersecting(circle))
            {
                if (tile.Tile.IsEmpty)
                    continue;

                toDestroy.Add((tile.GridIndices, Tile.Empty));
            }

            grid.SetTiles(toDestroy);
        }
    }

    /// <summary>
    /// Handle deleting entities in beam radius.
    /// </summary>
    private void DestroyEntities(IcarusBeamComponent component, TransformComponent trans)
    {
        var radius = component.DestroyRadius - 0.5f;
        foreach (var entity in _lookup.GetEntitiesInRange(trans.MapID, trans.WorldPosition, radius))
        {
            if (!CanDestroy(component, entity))
                continue;

            QueueDel(entity);
        }
    }

    /// <summary>
    /// Handle igniting flammable entities in beam radius.
    /// </summary>
    private void BurnEntities(IcarusBeamComponent component, TransformComponent trans)
    {
        var radius = component.FlameRadius * 2;
        foreach (var entity in _lookup.GetEntitiesInRange(trans.MapID, trans.WorldPosition, radius))
        {
            if (!CanDestroy(component, entity))
                continue;

            if (!TryComp<FlammableComponent>(entity, out var flammable))
                continue;

            flammable.FireStacks += 1;
            if (!flammable.OnFire)
                _flammable.Ignite(entity);
        }
    }

    private bool CanDestroy(IcarusBeamComponent component, EntityUid entity)
    {
        return entity != component.Owner &&
               !EntityManager.HasComponent<IMapGridComponent>(entity) &&
               !EntityManager.HasComponent<GhostComponent>(entity);
    }
}
