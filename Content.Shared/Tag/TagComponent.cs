using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tag;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(TagSystem))]
public sealed partial class TagComponent : Component
{
    [DataField, ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
}
