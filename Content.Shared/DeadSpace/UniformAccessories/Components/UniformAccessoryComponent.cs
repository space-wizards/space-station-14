using Robust.Shared.GameStates;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared.DeadSpace.UniformAccessories.Components;

[RegisterComponent] [NetworkedComponent] [AutoGenerateComponentState]
public sealed partial class UniformAccessoryComponent : Component
{
    /// <summary>
    /// The category of the accessory.
    /// </summary>
    [DataField] [AutoNetworkedField]
    public string Category = "";

    /// <summary>
    /// The color used for the accessory name in examine text.
    /// </summary>
    [DataField("color")]
    public Color? Color;

    /// <summary>
    /// Whether the accessory is drawn on the holder's item icon (in inventory/world).
    /// </summary>
    [DataField] [AutoNetworkedField]
    public bool DrawOnItemIcon = true;

    /// <summary>
    /// Whether the accessory can be drawn on the character's sprite.
    /// </summary>
    [DataField] [AutoNetworkedField]
    public bool HasIconSprite;

    /// <summary>
    /// Whether the accessory is hidden on the character's sprite.
    /// </summary>
    [DataField] [AutoNetworkedField]
    public bool Hidden;

    /// <summary>
    /// The explicit layer key for the accessory on the character's sprite.
    /// </summary>
    [DataField] [AutoNetworkedField]
    public string? LayerKey;

    /// <summary>
    /// The maximum number of accessories of this category allowed on the holder.
    /// </summary>
    [DataField] [AutoNetworkedField]
    public int Limit = 1;

    /// <summary>
    /// The sprite to use for the accessory on the character's sprite.
    /// </summary>
    [DataField] [AutoNetworkedField]
    public Rsi? PlayerSprite;
}
