using Robust.Shared.GameStates;
using System.Numerics;

namespace Content.Shared.Holopad;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolopadHologramComponent : Component
{
    [DataField]
    public string ShaderName;

    [DataField]
    public Color Color1 = Color.White;

    [DataField]
    public Color Color2 = Color.White;

    [DataField]
    public float Alpha = 1f;

    [DataField]
    public float Intensity = 1f;

    [DataField]
    public float ScrollRate = 0.1f;


    /// <summary>
    /// The sprite offset for the hologram
    /// </summary>
    [ViewVariables]
    public Vector2 Offset = new Vector2();
}
