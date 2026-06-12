using Content.Shared.Dataset;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Organs;

/// <summary>
/// Component that contributes sounds to a stethoscope reading when damaged
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(OffbrandDamageableOrganSoundsSystem))]
public sealed partial class OffbrandDamageableOrganSoundsComponent : Component
{
    /// <summary>
    /// Auditory descriptions to contribute to a sound when damaged
    /// </summary>
    [DataField(required: true)]
    public SortedDictionary<FixedPoint2, ProtoId<LocalizedDatasetPrototype>> Descriptions;
}
