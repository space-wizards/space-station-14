using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client.GreyStation.Clothing;

/// <summary>
/// Applies a shader overlay to the screen when worn.
/// </summary>
[RegisterComponent, Access(typeof(ShaderClothingSystem))]
public sealed partial class ShaderClothingComponent : Component
{
    /// <summary>
    /// The shader to use fullscreen.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ShaderPrototype> Shader = string.Empty;

    /// <summary>
    /// Shader instance created on mapinit.
    /// </summary>
    [ViewVariables]
    public ShaderInstance? Instance;
}
