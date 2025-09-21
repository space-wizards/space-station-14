using Content.Shared._Starlight.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tag;

[AutoGenerateComponentState(true), Access(typeof(TagSystem), typeof(StarlightSharedTagSystem))] // Starlight
[RegisterComponent, NetworkedComponent]
public sealed partial class TagComponent : Component
{
    [DataField, ViewVariables, AutoNetworkedField]
    public HashSet<ProtoId<TagPrototype>> Tags = new();
}
