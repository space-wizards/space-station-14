using System.Numerics;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Jittering;

/// <summary>
/// Marker component so that we can properly handle the jittering animation and returning it to where it started.
/// Should only be applied by <see cref="JitteringStatusEffectComponent"/>.
/// </summary>
/// <remarks>
/// This component breaks best practices for status effects, ideally it does not exist.
/// It's required for now due to prediction sometimes falsely raising
/// StatusEffectRemovedEvent by removing the status effect from its container before the effect is properly done.
/// </remarks>
[RegisterComponent, Access(typeof(SharedJitteringSystem))]
public sealed partial class JitteringComponent : Component
{
    /// <summary>
    /// The offset that an entity had before jittering so we can reset it properly.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 StartOffset;
}

/// <summary>
/// Causes the sprite of the status target to move around erratically.
/// Used in conjunction with <see cref="StatusEffectComponent"/> on a status effect entity.
/// </summary>
[Access(typeof(SharedJitteringSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JitteringStatusEffectComponent : Component
{
    /// <summary>
    /// Parameters for the behavior and movement of a jitter.
    /// </summary>
    [DataField, AutoNetworkedField]
    public JitterParameters Jitter = new();
}

[DataDefinition, Serializable, NetSerializable]
public partial struct JitterParameters()
{
    /// <summary>
    /// How many jitters will be preformed per second.
    /// </summary>
    [DataField]
    public float Frequency;

    /// <summary>
    /// The maximum distance the sprite will travel from the entity's actual position.
    /// </summary>
    [DataField]
    public float MaxRadius;

    /// <summary>
    /// The minimum distance to travel from origin.
    /// </summary>
    [DataField]
    public float MinRadius;

    /// <summary>
    /// The animation type to play.
    /// </summary>
    [DataField]
    public JitterType Type = JitterType.Line;

    /// <summary>
    /// Jitter offsets are transformed by this matrix to finely control potential destinations.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Matrix3x2 Matrix => Matrix3x2.Create(MatrixX, MatrixY, MatrixT);

    /// <summary>
    /// Sheer applied to the X coordinate.
    /// </summary>
    [DataField]
    public Vector2 MatrixX = Vector2.UnitX;

    /// <summary>
    /// Sheer applied to the Y coordinate.
    /// </summary>
    [DataField]
    public Vector2 MatrixY = Vector2.UnitY;

    /// <summary>
    /// A translation applied to the coordinates.
    /// </summary>
    [DataField]
    public Vector2 MatrixT = Vector2.Zero;
}

/// <summary>
/// Switch for different jitter animations.
/// </summary>
public enum JitterType
{
    // The jitter plays in straight lines from point to point
    Line,
    // The jitter adds a midpoint with the highest Y position of the three points
    Arch,
}
