using System.Linq;
using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using static Robust.Shared.Utility.SpriteSpecifier;
using Robust.Shared.Serialization;
using Content.Shared.Power;

namespace Content.Shared.Pinpointer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavMapTrackingConsoleComponent : Component
{
    [ViewVariables]
    [AutoNetworkedField]
    public List<NavMapTrackingData> TrackingData = new();
}

[Serializable, NetSerializable]
public struct NavMapTrackingData
{
    public NetEntity NetEntity;
    public NetCoordinates Coordinates;
    public ProtoId<NavMapTrackablePrototype> ProtoId;
    public Color Modulate = Color.White;
    public bool Blinks = false;

    public NavMapTrackingData(NetEntity netEntity, NetCoordinates coordinates, ProtoId<NavMapTrackablePrototype> protoId)
    {
        NetEntity = netEntity;
        Coordinates = coordinates;
        ProtoId = protoId;
    }
}

[Prototype("navMapTrackable")]
public sealed class NavMapTrackablePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("group")]
    public PowerMonitoringConsoleGroup Group;

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
    ///     Returns the texture for the associated map icon
    /// </summary>
    public Texture? Texture
    {
        get
        {
            if (_texture != null)
                return _texture;

            if (TexturePath.Any())
                _texture = new Texture(new(TexturePath));

            return _texture;
        }
    }

    private Texture? _texture;

    /// <summary>
    ///     Specifies the color of the associated map icon
    /// </summary>
    [DataField("color")]
    public Color Color;
}
