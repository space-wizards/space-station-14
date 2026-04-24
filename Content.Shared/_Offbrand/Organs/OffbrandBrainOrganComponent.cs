using Content.Shared._Offbrand.Maths;
using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.Organs;

[RegisterComponent, NetworkedComponent]
public sealed partial class OffbrandBrainOrganComponent : Component
{
    /// <summary>
    /// The curve to use for vascular tone.
    /// </summary>
    [DataField(required: true)]
    public ICurve VascularToneCurve;
}
