using Robust.Shared.GameStates;

namespace Content.Shared.Sprite;

/// <summary>
/// If your client entity is behind this then the sprite's alpha will be lowered so your entity remains visible.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpriteFadeComponent : Component
{
    /// <summary>
    ///     If true, fades the item even if there is nothing else clickable behind the hovered point.
    /// </summary>
    [DataField]
    public bool IgnoreClickableRestriction = false;
}
