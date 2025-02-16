using System.Numerics;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// When added to a map will apply shadows from <see cref="SunShadowComponent"/> to the lighting render target.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SunShadowComponent : Component
{
    /// <summary>
    /// Direction for the shadows to be extrapolated in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Direction;

    [DataField, AutoNetworkedField]
    public float Alpha;
}
