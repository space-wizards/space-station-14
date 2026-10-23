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
    /// Maximum length of <see cref="Direction"/>. Mostly used in context of querying for grids off-screen.
    /// </summary>
    public const float MaxLength = 5f;

    /// <summary>
    /// Direction for the shadows to be extrapolated in.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Direction;

    [DataField, AutoNetworkedField]
    public float Alpha;
}
