using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Administration.Components;

/// <summary>
/// Displays a sprite above an entity.
/// By default a huge sign saying "KILL".
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class KillSignComponent : Component
{
    /// <summary>
    /// The sprite show above the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Sprite = new SpriteSpecifier.Rsi(new ResPath("Objects/Misc/killsign.rsi"), "kill");

    /// <summary>
    /// Whether the granted layer should always be forced to be unshaded.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ForceUnshaded = true;

    /// <summary>
    /// Whether the granted layer should be offset to be above the entity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool DoOffset = true;

    /// <summary>
    /// Prevents the sign from displaying to the owner of the component, allowing everyone but them to see it.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool HideFromOwner = false;

    /// <summary>
    /// The scale of the sprite.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2 Scale = Vector2.One;
}
