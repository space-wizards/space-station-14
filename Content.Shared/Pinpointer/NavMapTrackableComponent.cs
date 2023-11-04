using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Pinpointer.UI;

/// <summary>
///     Entities with this component can appear on station navigation (nav) maps
/// </summary>
[RegisterComponent, AutoGenerateComponentState]
public sealed partial class NavMapTrackableComponent : Component
{
    [DataField("protoId", required: true), AutoNetworkedField]
    public ProtoId<NavMapTrackablePrototype> ProtoId;

    [AutoNetworkedField]
    public NetCoordinates Coordinates;

    [AutoNetworkedField]
    public List<NetCoordinates> ChildCoordinates = new();

    public Color Modulate = Color.White;
    public bool Blinks = false;
}
