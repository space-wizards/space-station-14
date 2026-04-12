using System.Numerics;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Jittering;

/// <summary>
/// Causes an entity's sprite to move erratically around their position.
/// Should only be applied to an entity by <see cref="JitteringStatusEffectComponent"/>.
/// </summary>
[Access(typeof(SharedJitteringSystem))]
[RegisterComponent]
public sealed partial class JitteringComponent : Component
{
    /// <summary>
    /// The current position of the sprite.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 LastJitter;

    // todo Saving this offset can cause a sprite to get stuck in a wierd spot, but requires animation deltas to solve
    /// <summary>
    /// The offset that an entity had before jittering started,
    /// so that we can reset it properly.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 StartOffset = Vector2.Zero;
}

/// <summary>
/// Applies <see cref="JitteringComponent"/> to the parent entity.
/// Use only in conjunction with <see cref="StatusEffectComponent"/> on a status effect entity.
/// </summary>
[RegisterComponent]
public sealed partial class JitteringStatusEffectComponent : Component
{
    /// <summary>
    /// The parameters of the jitter to apply.
    /// </summary>
    [DataField]
    public JitterParams Settings = new(3, 0.25f, 0);
}

[Serializable, NetSerializable]
public struct JitterParams(float frequency, float maxRadius, float minRadius)
{
    /// <summary>
    /// How many jitters will be preformed per second.
    /// </summary>
    public float Frequency = frequency;

    /// <summary>
    /// The maximum distance the sprite will travel from the entity's actual position.
    /// </summary>
    public float MaxRadius = maxRadius;

    /// <summary>
    /// The minimum distance to travel from origin.
    /// </summary>
    public float MinRadius = minRadius;

    /// <summary>
    /// A linear transformation to apply to X.
    /// </summary>
    public Vector2 XSheer = Vector2.UnitX;

    /// <summary>
    /// A linear transformation to apply to Y.
    /// </summary>
    public Vector2 YSheer =  Vector2.UnitY;

    public Matrix3x2 MovementMatrix => Matrix3x2.Create(XSheer, YSheer, Vector2.Zero);
}
