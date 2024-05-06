using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Wounding.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Medical.Wounding.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(WoundSystem))]
public sealed partial class WoundComponent : Component
{
    /// <summary>
    /// This is the body we are attached to, if we are attached to one
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// Root woundable for our parent, this will always be valid
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid RootEntity;

    /// <summary>
    /// Current parentWoundable, this will always be valid
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly), AutoNetworkedField]
    public EntityUid ParentWoundable;

    /// <summary>
    /// The current severity of the wound expressed as a percentage (/100).
    /// This is used to modify multiple values.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public FixedPoint2 Severity = 100;

    /// <summary>
    /// Whether multiple wounds originating from the same prototype can exist on a woundable.
    /// </summary>
    [DataField]
    public bool Unique;
}
