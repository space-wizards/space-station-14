using Robust.Shared.Random;

namespace Content.Shared.ValueSelectors.Floats;

/// <summary>
/// Used for implementing custom value selection of <see cref="float"/>s.
/// </summary>
public abstract partial class FloatSelector : IBaseValueSelector<float, float>
{
    /// <inheritdoc/>
    public abstract float Get(IRobustRandom rand);

    /// <inheritdoc/>
    public abstract float Odds();

    /// <inheritdoc/>
    public abstract float Average();
}
