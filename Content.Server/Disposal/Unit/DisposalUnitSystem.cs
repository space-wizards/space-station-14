using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Destructible;
using Content.Shared.Disposal.Components;
using Content.Shared.Disposal.Unit;
using Content.Shared.Explosion;

namespace Content.Server.Disposal.Unit;

public sealed class DisposalUnitSystem : SharedDisposalUnitSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DisposalUnitComponent, DestructionEventArgs>(OnDestruction);
        SubscribeLocalEvent<DisposalUnitComponent, BeforeExplodeEvent>(OnExploded);
    }

    protected override void HandleAir(EntityUid uid, DisposalUnitComponent component, TransformComponent xform)
    {
        var air = component.Air;
        var indices = TransformSystem.GetGridTilePositionOrDefault((uid, xform));

        if (_atmosSystem.GetTileMixture(xform.GridUid, xform.MapUid, indices, true) is { Temperature: > 0f } environment)
        {
            var transferMoles = 0.1f * (0.25f * Atmospherics.OneAtmosphere * 1.01f - air.Pressure) * air.Volume / (environment.Temperature * Atmospherics.R);

            component.Air = environment.Remove(transferMoles);
        }
    }

    private void OnDestruction(EntityUid uid, DisposalUnitComponent component, DestructionEventArgs args)
    {
        TryEjectContents(uid, component);
    }

    private void OnExploded(Entity<DisposalUnitComponent> ent, ref BeforeExplodeEvent args)
    {
        args.Contents.AddRange(ent.Comp.Container.ContainedEntities);
    }
}
