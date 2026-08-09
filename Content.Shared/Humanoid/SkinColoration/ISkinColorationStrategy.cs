using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Serialization;

namespace Content.Shared.Humanoid.SkinColoration;

/// <summary>
/// Takes in the given <see cref="SkinColorationStrategyInput" /> and returns an adjusted Color
/// </summary>
public interface ISkinColorationStrategy
{
    /// <summary>
    /// The type of input expected by the implementor; callers should consult InputType before calling the methods that require a given input
    /// </summary>
    SkinColorationStrategyInput InputType { get; }

    /// <summary>
    /// Returns whether or not the provided <see cref="Color" /> is within bounds of this strategy
    /// Outs a reason if the verification fails.
    /// </summary>
    bool VerifySkinColor(Color color, [NotNullWhen(false)] out string? reason);

    /// <summary>
    /// Returns the closest skin color that this strategy would provide to the given <see cref="Color" />
    /// </summary>
    Color ClosestSkinColor(Color color);

    /// <summary>
    /// Returns the input if it passes <see cref="VerifyClampedSkinColor">, otherwise returns <see cref="ClosestSkinColor" />
    /// </summary>
    Color EnsureVerified(Color color)
    {
        if (VerifyClampedSkinColor(color, out _))
        {
            return color;
        }

        return ClosestSkinColor(color);
    }

    /// <summary>
    /// Returns if the color, or any nearby, is valid.
    /// Due to RGB truncation, clamped colors near a threshold may be out of spec,
    /// so we brute force checking nearby colors.
    /// </summary>
    bool VerifyClampedSkinColor(Color color, [NotNullWhen(false)] out string? reason)
    {
        string firstReason = string.Empty;
        for (int i = 0; i < 8; i++)
        {
            Color testColor = color;
            if ((i & 1) != 0)
                color.R = Math.Min(color.R + SkinColorationUtils.Epsilon, 1.0f);
            if ((i & 2) != 0)
                color.G = Math.Min(color.G + SkinColorationUtils.Epsilon, 1.0f);
            if ((i & 4) != 0)
                color.B = Math.Min(color.B + SkinColorationUtils.Epsilon, 1.0f);

            if (VerifySkinColor(testColor, out var internalReason))
            {
                reason = null;
                return true;
            }

            firstReason ??= internalReason;
        }

        reason = firstReason;
        return false;
    }

    /// <summary>
    /// Returns a colour representation of the given unary input
    /// </summary>
    Color FromUnary(float unary)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }

    /// <summary>
    /// Returns a colour representation of the given unary input
    /// </summary>
    float ToUnary(Color color)
    {
        throw new InvalidOperationException("This coloration strategy does not support unary input");
    }
}

/// <summary>
/// The type of input taken by a <see cref="ISkinColorationStrategy" />
/// </summary>
[Serializable, NetSerializable]
public enum SkinColorationStrategyInput
{
    /// <summary>
    /// A single floating point number from 0 to 100 (inclusive)
    /// </summary>
    Unary,

    /// <summary>
    /// A <see cref="Color" />
    /// </summary>
    Color,
}
