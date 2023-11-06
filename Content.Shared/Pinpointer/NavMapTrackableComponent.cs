using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Pinpointer.UI;

/// <summary>
///     Entities with this component can appear on station navigation (nav) maps
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NavMapTrackableComponent : Component
{
    [DataField("protoId", required: true), AutoNetworkedField]
    public ProtoId<NavMapTrackablePrototype> ProtoId;

    [ViewVariables, AutoNetworkedField]
    public List<NetCoordinates> ChildPositionOffsets = new();

    public Color Modulate = Color.White;
    public bool Blinks = false;
}
