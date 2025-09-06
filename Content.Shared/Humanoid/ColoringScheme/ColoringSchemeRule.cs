using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared.Humanoid.ColoringScheme;

/// <summary>
/// Abstract rule for validating, clamping, and randomizing colors for a specific part of a coloring scheme.
/// </summary>
[ImplicitDataDefinitionForInheritors]
[MeansImplicitUse]
public abstract partial class ColoringSchemeRule
{
    /// <summary>
    /// Verify that the color is valid according to this rule.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public abstract bool Verify(Color color);

    /// <summary>
    /// Clamp the color to be valid according to this rule.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public abstract Color Clamp(Color color);

    /// <summary>
    /// Generate a random color according to this rule.
    /// </summary>
    /// <param name="random"></param>
    /// <returns></returns>
    public abstract Color Randomize(IRobustRandom random);
}
