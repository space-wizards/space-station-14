using Content.Shared.Hands.Components;
using Content.Shared.Item;
using Robust.Shared.GameStates;

namespace Content.Shared.Sprite;

/// <summary>
/// If your client entity is behind this then the sprite's alpha will be lowered so your entity remains visible.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpriteFadeComponent : Component
{
    [DataField("ignoreClickableRestrict")]
    public bool IgnoreClickableRestriction = false;
}
