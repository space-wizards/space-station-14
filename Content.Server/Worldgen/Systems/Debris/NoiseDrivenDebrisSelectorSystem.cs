using Content.Server.Worldgen.Components.Debris;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems.Debris;

/// <summary>
///     This handles selecting debris with probability decided by a noise channel.
/// </summary>
public sealed class NoiseDrivenDebrisSelectorSystem : BaseWorldSystem
{
    [Dependency] private readonly NoiseIndexSystem _index = default!;
    [Dependency] private readonly TransformSystem _xformSys = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private ISawmill _sawmill = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        _sawmill = _logManager.GetSawmill("world.debris.noise_debris_selector");
        // Event is forcibly ordered to always be handled after the simple selector.
        SubscribeLocalEvent<NoiseDrivenDebrisSelectorComponent, TryGetPlaceableDebrisFeatureEvent>(OnSelectDebrisKind,
            after: new[] {typeof(DebrisFeaturePlacerSystem)});
    }

    private void OnSelectDebrisKind(EntityUid uid, NoiseDrivenDebrisSelectorComponent component,
        ref TryGetPlaceableDebrisFeatureEvent args)
    {
        var coords = WorldGen.WorldToChunkCoords(_xformSys.ToMapCoordinates(args.Coords).Position);
        var prob = _index.Evaluate(uid, component.NoiseChannel, coords);

        if (prob is < 0 or > 1)
        {
            _sawmill.Error(
                $"Sampled a probability of {prob}, which is outside the [0, 1] range, at {coords} aka {args.Coords}.");
            return;
        }

        if (!_random.Prob(prob))
            return;

        var l = new List<string?>(1);
        component.CachedDebrisTable.GetSpawns(_random, ref l);

        switch (l.Count)
        {
            case 0:
                return;
            case > 1:
                _sawmill.Warning($"Got more than one possible debris type from {uid}. List: {string.Join(", ", l)}");
                break;
        }

        args.DebrisProto = l[0];
    }
}

