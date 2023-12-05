using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using static Robust.Shared.Utility.SpriteSpecifier;
using System.Numerics;

namespace Content.Shared.Pinpointer;

/// <summary>
///     Entities with this component can appear on station navigation (nav) maps
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NavMapTrackableComponent : Component
{
    /// <summary>
    ///     Sets the texture path for the associated nav map icon
    /// </summary>
    [DataField("texture")]
    public string TexturePath
    {
        set
        {
            _texturePath = value;
            _texture = null;
        }

        get
        {
            return _texturePath;
        }
    }

    private string _texturePath = string.Empty;

    /// <summary>
    ///     Returns the texture for the associated nav map icon
    /// </summary>
    public Texture? Texture
    {
        get
        {
            if (_texture != null)
                return _texture;

            if (TexturePath.Length > 0)
                _texture = new Texture(new(TexturePath));

            return _texture;
        }
    }

    private Texture? _texture;

    /// <summary>
    ///     Specifies the primary color of the associated nav map icon and retrives its modulated color
    /// </summary>
    [DataField("color")]
    public Color Color
    {
        get { return _color * Modulate; }

        set { _color = value; }
    }

    private Color _color;

    /// <summary>
    ///     This can be used to modulate the primary color of the icon drawn on the nav map
    /// </summary>
    public Color Modulate = Color.White;

    /// <summary>
    ///     This indicates whether the icon drawn on the nav map should blink on and off 
    /// </summary>
    public bool Blinks = false;

    /// <summary>
    ///     The parent that will handle the drawing of this entity on the nav map
    /// </summary>
    /// <remarks>
    ///     Make sure that this entity position offset is added to the parent's NavMapTrackableComponent
    /// </remarks>
    [ViewVariables]
    public EntityUid? ParentUid;

    /// <summary>
    ///     Specifies the coordinate offsets for any children attached to this entity
    /// </summary>
    /// <remarks>
    ///     This allows you to draw multiple icons on a nav map via a single component 
    /// </remarks>
    [ViewVariables]
    public List<Vector2> ChildOffsets = new();
}

[Serializable, NetSerializable]
public sealed class NavMapTrackableComponentState : ComponentState
{
    public NetEntity? ParentUid { get; init; }
    public List<Vector2> ChildOffsets { get; init; } = new();
}
