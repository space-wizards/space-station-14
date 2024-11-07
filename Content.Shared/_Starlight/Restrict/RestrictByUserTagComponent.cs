using Content.Shared._Starlight.Antags.Abductor;
using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Starlight.Restrict;
[RegisterComponent, NetworkedComponent, Access(typeof(SharedAbductorSystem)), AutoGenerateComponentState]
public sealed partial class RestrictByUserTagComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Contains = [];

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> DoestContain = [];

    [DataField, AutoNetworkedField]
    public List<string> Messages = [];
}