using Content.Server.GameTicking.Rules.VariationPass.Components;
using Content.Server.GameTicking.Rules.VariationPass.Components.MisplacementMarker;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules.VariationPass;

/// <summary>
///     Tries to moves entities to a random non-spaced tile.
///     Not optimized to be used for a lot of entities!
///     Entities moved by this component require a <see cref="MisplacementMarkerComponent"/>.
/// </summary>
///
public sealed class EntityMisplacementVariationPassSystem : VariationPassSystem<EntityMisplacementVariationPassComponent>
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    protected override void ApplyVariation(Entity<EntityMisplacementVariationPassComponent> ent, ref StationVariationPassEvent args)
    {
        var enumerator = AllEntityQuery<MisplacementMarkerComponent, TransformComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var xform))
        {
            var protoId = MetaData(uid).EntityPrototype;
            if (protoId == null || protoId != ent.Comp.MisplacedEntity)
                continue;

            if (!IsMemberOfStation((uid, xform), ref args))
                continue;

            // can't find a place to spawn, do nothing
            if (!TryFindRandomTileOnStation(args.Station, out _, out _, out var coords))
                return;

            if (ent.Comp.ReplacementEntity != null)
            {
                if (TryComp(uid, out TransformComponent? comp))
                    _entManager.SpawnEntity(ent.Comp.ReplacementEntity, comp.Coordinates);
            }

            _transform.SetCoordinates(uid, coords);
        }
    }
}
