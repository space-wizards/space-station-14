using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Offbrand.Wounds;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HeartrateAlertsSystem))]
public sealed partial class HeartrateAlertsComponent : Component
{
    /// <summary>
    /// Alert displayed if the heart is running, severity increases with strain
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertPrototype> StrainAlert;

    /// <summary>
    /// Alert displayed if the heart is stopped
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertPrototype> StoppedAlert;

    /// <summary>
    /// The category of the alerts
    /// </summary>
    [DataField(required: true)]
    public ProtoId<AlertCategoryPrototype> AlertCategory;

    /// <summary>
    /// The maximum strain for the <see cref="StrainAlert" />
    /// </summary>
    [DataField(required: true)]
    public FixedPoint2 MaxStrain;
}
