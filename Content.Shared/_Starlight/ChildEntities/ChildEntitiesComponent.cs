using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._Starlight;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ChildEntitiesComponent : Component
{
    [DataField]
    public List<ChildEntityInfo> ChildPrototypes = [];

    [DataField, AutoNetworkedField]
    public List<EntityUid> Children = [];
}

[Serializable, NetSerializable, DataDefinition]
public sealed partial class ChildEntityInfo
{
    [DataField]
    public EntProtoId Prototype;

    [DataField]
    public Vector2 Offset;
}