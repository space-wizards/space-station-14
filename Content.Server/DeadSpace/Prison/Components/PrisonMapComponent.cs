using Content.Shared.DeadSpace.Prison;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Prison.Components;

[RegisterComponent]
public sealed partial class PrisonMapComponent : Component
{
    [DataField]
    public ProtoId<PrisonPlanetPrototype> Planet;
}
