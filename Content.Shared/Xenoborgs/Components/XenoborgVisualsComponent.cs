using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Xenoborgs.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class XenoborgVisualsComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? FallbackSprite = null;

    /// <summary>
    /// The key of the entity layer into which the sprite will be inserted
    /// </summary>
    [DataField]
    public string LayerMap = "unshaded_lighting";

    [DataField, AutoNetworkedField]
    public bool ForceUnshaded = true;
}
