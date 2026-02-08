using System.Numerics;
using Content.Shared.Light.Components;
using Content.Shared.StatusEffectNew.Components;
using Content.Shared.Weather;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Player;

namespace Content.Client.Weather;

public sealed class WeatherSystem : SharedWeatherSystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly MapSystem _mapSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<AudioComponent> _audioQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<RoofComponent> _roofQuery;

    public override void Initialize()
    {
        base.Initialize();

        _audioQuery = GetEntityQuery<AudioComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _roofQuery = GetEntityQuery<RoofComponent>();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var ent = _playerManager.LocalEntity;

        if (ent == null)
            return;

        if (!Timing.IsFirstTimePredicted)
            return;

        var entXform = Transform(ent.Value);

        var query = EntityQueryEnumerator<WeatherStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var uid, out var weather, out var status))
        {
            if (weather.Sound == null)
                return;

            weather.Stream ??= _audio.PlayGlobal(weather.Sound, Filter.Local(), true)?.Entity;

            if (!_audioQuery.TryComp(weather.Stream, out var audio))
                return;

            var occlusion = 0f;

            // Work out tiles nearby to determine volume.
            if (_gridQuery.TryComp(entXform.GridUid, out var grid))
            {
                _roofQuery.TryComp(entXform.GridUid, out var roofComp);
                var gridId = entXform.GridUid.Value;
                // FloodFill to the nearest tile and use that for audio.
                var seed = _mapSystem.GetTileRef(gridId, grid, entXform.Coordinates);
                var frontier = new Queue<TileRef>();
                frontier.Enqueue(seed);
                // If we don't have a nearest node don't play any sound.
                EntityCoordinates? nearestNode = null;
                var visited = new HashSet<Vector2i>();

                while (frontier.TryDequeue(out var node))
                {
                    if (!visited.Add(node.GridIndices))
                        continue;

                    if (!CanWeatherAffect((entXform.GridUid.Value, grid, roofComp), node))
                    {
                        // Add neighbors
                        // TODO: Ideally we pick some deterministically random direction and use that
                        // We can't just do that naively here because it will flicker between nearby tiles.
                        for (var x = -1; x <= 1; x++)
                        {
                            for (var y = -1; y <= 1; y++)
                            {
                                if (Math.Abs(x) == 1 && Math.Abs(y) == 1 ||
                                    x == 0 && y == 0 ||
                                    (new Vector2(x, y) + node.GridIndices - seed.GridIndices).Length() > 3)
                                {
                                    continue;
                                }

                                frontier.Enqueue(_mapSystem.GetTileRef(gridId, grid, new Vector2i(x, y) + node.GridIndices));
                            }
                        }

                        continue;
                    }

                    nearestNode = new EntityCoordinates(entXform.GridUid.Value,
                        node.GridIndices + grid.TileSizeHalfVector);
                    break;
                }

                // Get occlusion to the targeted node if it exists, otherwise set a default occlusion.
                if (nearestNode != null)
                {
                    var entPos = _transform.GetMapCoordinates(entXform);
                    var nodePosition = _transform.ToMapCoordinates(nearestNode.Value).Position;
                    var delta = nodePosition - entPos.Position;
                    var distance = delta.Length();
                    occlusion = _audio.GetOcclusion(entPos, delta, distance);
                }
                else
                {
                    occlusion = 3f;
                }
            }

            var alpha = GetWeatherPercent((uid, status));
            alpha *= SharedAudioSystem.VolumeToGain(weather.Sound.Params.Volume);
            _audio.SetGain(weather.Stream, alpha, audio);
            audio.Occlusion = occlusion;
        }
    }
}
