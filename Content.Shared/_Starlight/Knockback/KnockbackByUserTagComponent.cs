using Content.Shared.Tag;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Starlight.Knockback;
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class KnockbackByUserTagComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> Contains = [];

    [DataField, AutoNetworkedField]
    public List<ProtoId<TagPrototype>> DoestContain = [];

    [DataField, AutoNetworkedField]
    public float Knockback = 0;
}
