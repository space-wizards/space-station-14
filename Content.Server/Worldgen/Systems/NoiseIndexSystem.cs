using System.Numerics;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Worldgen.Systems;

/// <summary>
///     This handles the noise index.
/// </summary>
public sealed class NoiseIndexSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    /// <summary>
    ///     Gets a particular noise channel from the index on the given entity.
    /// </summary>
    /// <param name="holder">The holder of the index</param>
    /// <param name="protoId">The channel prototype ID</param>
    /// <returns>An initialized noise generator</returns>
    public NoiseGenerator Get(EntityUid holder, string protoId)
    {
        var idx = EnsureComp<NoiseIndexComponent>(holder);
        if (idx.Generators.TryGetValue(protoId, out var generator))
            return generator;
        var proto = _prototype.Index<NoiseChannelPrototype>(protoId);
        var gen = new NoiseGenerator(proto, _random.Next());
        idx.Generators[protoId] = gen;
        return gen;
    }

    /// <summary>
    ///     Attempts to evaluate the given noise channel using the generator on the given entity.
    /// </summary>
    /// <param name="holder">The holder of the index</param>
    /// <param name="protoId">The channel prototype ID</param>
    /// <param name="coords">The coordinates to evaluate at</param>
    /// <returns>The result of evaluation</returns>
    public float Evaluate(EntityUid holder, string protoId, Vector2 coords)
    {
        var gen = Get(holder, protoId);
        return gen.Evaluate(coords);
    }
}

