using Robust.Shared.GameStates;

namespace Content.Shared.Paint;

[RegisterComponent, NetworkedComponent]
[Access(typeof(PaintSystem))]
public sealed partial class PaintedComponent : Component
{
    /// <summary>
    /// The color that is applied to the entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public Color Color = Color.Yellow;

    /// <summary>
    /// The shader that is applied to the entity.
    /// </summary>
    [DataField]
    public string ShaderName = "Greyscale";
}

