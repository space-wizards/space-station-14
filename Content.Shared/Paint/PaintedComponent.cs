using Robust.Shared.GameStates;
using Content.Shared.Decals;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

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
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color = Color.FromHex("#2cdbd5");

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

