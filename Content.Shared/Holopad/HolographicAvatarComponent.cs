using Robust.Shared.GameStates;

namespace Content.Shared.Holopad;

[RegisterComponent, NetworkedComponent]
public sealed partial class HolographicAvatarComponent : Component
{
    /// <summary>
    /// The prototype sprite layer data for the hologram
    /// </summary>
    [DataField]
    public PrototypeLayerData[] LayerData;
}
