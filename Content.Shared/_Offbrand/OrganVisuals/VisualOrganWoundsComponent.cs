using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Offbrand.OrganVisuals;

[RegisterComponent, NetworkedComponent]
public sealed partial class VisualOrganWoundsComponent : Component
{
    [DataField(required: true)]
    public ResPath MaskPath;

    [DataField(required: true)]
    public List<VisualOrganWoundsDamageGroup> DamageGroups;
}

[DataDefinition]
public sealed partial class VisualOrganWoundsDamageGroup
{
    [DataField(required: true)]
    public ProtoId<DamageGroupPrototype> DamageGroup;

    [DataField(required: true)]
    public ResPath OverlayPath;

    [DataField]
    public Color? Color;
}
