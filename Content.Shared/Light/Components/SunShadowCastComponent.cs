using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;

namespace Content.Shared.Light.Components;

/// <summary>
/// Treats this entity as a 1x1 tile and extrapolates its position along the <see cref="SunShadowComponent"/> direction.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SunShadowCastComponent : Component
{
    /// <summary>
    /// Points that will be extruded to draw the shadow color.
    /// Max <see cref="PhysicsConstants.MaxPolygonVertices"/>
    /// </summary>
    [DataField]
    public Vector2[] Points = new[]
    {
        new Vector2(-0.5f, -0.5f),
        new Vector2(0.5f, -0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(-0.5f, 0.5f),
    };
}
