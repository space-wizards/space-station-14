// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Necromorphs.Unitology;

namespace Content.Server.DeadSpace.Necromorphs.Unitology.Components;

[RegisterComponent, Access(typeof(UnitologySubmissionConditionsSystem))]
public sealed partial class UnitologySubmissionConditionsComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public float Progress = 0;
}
