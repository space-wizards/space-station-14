using System.Linq;
using Content.Shared.Pinpointer;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Server.Pinpointer.UI;

/// <summary>
///     Entities with this component can appear on station navigation (nav) maps
/// </summary>
[RegisterComponent]
public sealed partial class NavMapTrackableComponent : Component
{
    [DataField("protoId", required: true)]
    public ProtoId<NavMapTrackablePrototype> ProtoId;

    /*/// <summary>
    ///     Determines if the icon should be rendered on the nav map
    /// </summary>
    [DataField("visible")]
    public bool Visible = true;

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

    private string _texturePath;

    /// <summary>
    ///     Returns the texture for the associated map icon
    /// </summary>
    public Texture? Texture
    {
        get
        {
            if (_texture != null)
                return _texture;

            if (TexturePath.Any())
                _texture = new Texture(new (TexturePath));

            return _texture;
        }
    }

    private Texture? _texture;

    /// <summary>
    ///     Specifies the color of the associated map icon
    /// </summary>
    [DataField("color")]
    public Color Color
    {
        set
        {
            _color = value;
        }

        get
        {
            return _color * Modulate;
        }
    }

    private Color _color = Color.White;

    /// <summary>
    ///     Modulates the primary color of the associated map icon
    /// </summary>
    /// <remarks>
    ///     Modulation is multiplying or tinting the color basically.
    /// </remarks>
    [DataField("modulate")]
    public Color Modulate = Color.White;

    /// <summary>
    ///     Determines if the associated map icon should blink on/off
    /// </summary>
    [DataField("blinks")]
    public bool Blinks = false;*/
}
