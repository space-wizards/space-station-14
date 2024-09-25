using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Holopad;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolopadHologramComponent : Component
{
    /// <summary>
    /// Name of the shader to use
    /// </summary>
    [DataField]
    public string ShaderName;

    /// <summary>
    /// The primary color
    /// </summary>
    [DataField]
    public Color Color1 = Color.White;

    /// <summary>
    /// The secondary color
    /// </summary>
    [DataField]
    public Color Color2 = Color.White;

    /// <summary>
    /// The shared color alpha
    /// </summary>
    [DataField]
    public float Alpha = 1f;

    /// <summary>
    /// The color brightness
    /// </summary>
    [DataField]
    public float Intensity = 1f;

    /// <summary>
    /// The scroll rate of the hologram shader
    /// </summary>
    [DataField]
    public float ScrollRate = 0.1f;

    /// <summary>
    /// The sprite offset
    /// </summary>
    [DataField]
    public Vector2 Offset = new Vector2();

    /// <summary>
    /// A set of entities that are linked to this hologram
    /// </summary>
    [ViewVariables]
    public HashSet<EntityUid> LinkedEntities = new();
}
