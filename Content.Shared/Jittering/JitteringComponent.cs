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
/// Removing this component requires either animation deltas or better container system prediction, or both.
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
    [DataField(required: true), AutoNetworkedField]
    public JitterParameters Jitter;
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
