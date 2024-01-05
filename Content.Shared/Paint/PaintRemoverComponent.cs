using Robust.Shared.GameStates;
using Content.Shared.Decals;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Audio;

namespace Content.Shared.Paint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PaintRemoverComponent : Component
{
    /// <summary>
    /// The color that is applied to the entity.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public Color Color = Color.FromHex("#e9be1a");

    [DataField, AutoNetworkedField]
    public bool Enabled;

    [DataField]
    public Color BeforePaintedColor;

    [DataField]
    public string? BeforePaintedShader;

    /// <summary>
    /// The shader that is applied to the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ShaderName = "Greyscale";
}

