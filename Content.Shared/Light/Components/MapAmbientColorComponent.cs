using Content.Shared.CCVar;
using Robust.Shared.GameStates;

namespace Content.Shared.Light.Components;

/// <summary>
/// Controls map-specific ambient occlusion color.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class MapAmbientColorComponent : Component
{
    /// <summary>
    /// Color used by ambient occlusion on this map.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Color Color = CCVars.DefaultAmbientOcclusionColor;
}
