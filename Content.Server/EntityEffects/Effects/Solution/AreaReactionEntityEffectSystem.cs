using Content.Server.Fluids.EntitySystems;
using Content.Server.Spreader;
using Content.Shared.Chemistry.Components;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.EntityEffects;
using Content.Shared.EntityEffects.Effects.Solution;
using Content.Shared.Maps;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;

namespace Content.Server.EntityEffects.Effects.Solution;

/// <summary>
/// This effect creates smoke at this solution's position.
/// The amount of smoke created is modified by scale.
/// </summary>
/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class AreaReactionEntityEffectsSystem : EntityEffectSystem<SolutionComponent, AreaReactionEffect>
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly SpreaderSystem _spreader = default!;
    [Dependency] private readonly TurfSystem _turf = default!;

    // TODO: A sane way to make Smoke without a solution.
    protected override void Effect(Entity<SolutionComponent> entity, ref EntityEffectEvent<AreaReactionEffect> args)
    {
        var xform = Transform(entity);
        var mapCoords = _xform.GetMapCoordinates(entity);
        var spreadAmount = (int) Math.Max(0, Math.Ceiling(args.Scale / args.Effect.OverflowThreshold));
        var effect = args.Effect;

        if (!_mapManager.TryFindGridAt(mapCoords, out var gridUid, out var grid) ||
            !_map.TryGetTileRef(gridUid, grid, xform.Coordinates, out var tileRef))
            return;

        if (_spreader.RequiresFloorToSpread(effect.PrototypeId.ToString()) && _turf.IsSpace(tileRef))
            return;

        var coords = _map.MapToGrid(gridUid, mapCoords);
        var ent = Spawn(args.Effect.PrototypeId, coords.SnapToGrid());

        _smoke.StartSmoke(ent, entity.Comp.Solution, args.Effect.Duration, spreadAmount);

        _audio.PlayPvs(args.Effect.Sound, entity, AudioParams.Default.WithVariation(0.25f));
    }
}
