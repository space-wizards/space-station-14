using System.Numerics;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Jittering;

/// <summary>
/// Marker component for an entity's sprite to move erratically around their position.
/// Should only be applied to an entity by <see cref="JitteringStatusEffectComponent"/>.
/// </summary>
/// <remarks>
/// This component only exists to keep track of where to reset the sprite.
/// With animation deltas this component can be removed.
/// </remarks>
[Access(typeof(SharedJitteringSystem))]
[RegisterComponent]
public sealed partial class JitteringComponent : Component
{
    /// <summary>
    /// The offset that an entity had before jittering started,
    /// so that we can reset it properly.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public Vector2 StartOffset = Vector2.Zero;
}

/// <summary>
/// Causes the sprite of the status target to move around erratically.
/// Use only in conjunction with <see cref="StatusEffectComponent"/> on a status effect entity.
/// </summary>
[Access(typeof(SharedJitteringSystem))]
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class JitteringStatusEffectComponent : Component
{
    /// <summary>
    /// The parameters of the jitter to apply.
    /// </summary>
    [DataField, AutoNetworkedField]
    public JitterSetting Settings = new()
    {
        Frequency = 3,
        MaxRadius = 0.15f,
        MinRadius = 0.05f,
    };
}

[DataDefinition, Serializable, NetSerializable]
public partial struct JitterSetting()
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
    public Matrix3x2 Matrix => Matrix3x2.Create(MatrixX, MatrixY, Vector2.Zero);

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

    // Too buggy without animation deltas
    // /// <summary>
    // /// A translation applied to the coordinates.
    // /// </summary>
    // [DataField]
    // public Vector2 MatrixT = Vector2.UnitX;
}
