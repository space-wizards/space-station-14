using System.Numerics;
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
    /// Clamp the color to be valid according to this rule.
    /// </summary>
    /// <param name="color"></param>
    /// <returns></returns>
    public abstract Color Clamp(Color color);

    protected static void Swap(ref float a, ref float b)
    {
        (a, b) = (b, a);
    }
}
