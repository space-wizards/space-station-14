using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Medical.Wounding.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WoundSystem))]
public sealed partial class WoundComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Body;

    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? RootWoundable;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Severity = 100;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxHealthDamage;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxIntegrityDamage;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxHealthDebuff;

    [DataField(required: true), ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 MaxIntegrityDebuff;

    public FixedPoint2 HealthDamage => Severity * MaxHealthDamage / 100;
    public FixedPoint2 IntegrityDamage => Severity * MaxIntegrityDamage / 100;

    public FixedPoint2 HealthDebuff => Severity * MaxHealthDebuff / 100;
    public FixedPoint2 IntegrityDebuff => Severity * MaxIntegrityDebuff / 100;
}
