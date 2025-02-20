using System.Numerics;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Item.ItemToggle.Components;

/// <summary>
///   Handles the embeddable stats for activated items.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ItemToggleEmbeddableProjectileComponent : Component
{
    /// <summary>
    ///   The removal time when this item is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? ActivatedRemovalTime;

    /// <summary>
    ///   The offset of the sprite when this item is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2? ActivatedOffset;

    /// <summary>
    ///   Whether this entity will embed when thrown when this item is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool? ActivatedEmbedOnThrow;

    /// <summary>
    ///   The sound to play after embedding when this item is activated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? ActivatedSound;

    /// <summary>
    ///   The removal time when this item is deactivated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float? DeactivatedRemovalTime;

    /// <summary>
    ///   The offset of the sprite when this item is deactivated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Vector2? DeactivatedOffset;

    /// <summary>
    ///   Whether this entity will embed when thrown when this item is deactivated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool? DeactivatedEmbedOnThrow;

    /// <summary>
    ///   The sound to play after embedding when this item is deactivated.
    /// </summary>
    [DataField, AutoNetworkedField]
    public SoundSpecifier? DeactivatedSound;
}
