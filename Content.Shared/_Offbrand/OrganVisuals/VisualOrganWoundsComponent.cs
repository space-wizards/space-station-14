using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
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

    [DataField(required: true)]
    public List<FixedPoint2> Thresholds;
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
