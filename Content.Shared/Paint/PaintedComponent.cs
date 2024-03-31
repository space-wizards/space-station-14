using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Paint;

/// <summary>
/// Component applied to target entity when painted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintedComponent : Component
{
    /// <summary>
    ///  Color of the paint.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#2cdbd5");

    /// <summary>
    ///  Used to remove the color when component removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color BeforeColor;

    /// <summary>
    /// If paint is enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Enabled;

    /// <summary>
    /// Name of the shader.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ShaderName = "Greyscale";
}

[Serializable, NetSerializable]
public enum PaintVisuals : byte
{
    Painted,
}
