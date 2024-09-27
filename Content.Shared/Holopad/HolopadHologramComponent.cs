using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Holopad;

/// <summary>
/// Holds data pertaining to holopad holograms
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolopadHologramComponent : Component
{
    /// <summary>
    /// Default RSI path
    /// </summary>
    [DataField]
    public string RsiPath;

    /// <summary>
    /// Default RSI state
    /// </summary>
    [DataField]
    public string RsiState;

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
    public float ScrollRate = 1f;

    /// <summary>
    /// The sprite offset
    /// </summary>
    [DataField]
    public Vector2 Offset = new Vector2();

    /// <summary>
    /// A user that are linked to this hologram
    /// </summary>
    [ViewVariables]
    public Entity<HolopadComponent>? LinkedHolopad;
}
