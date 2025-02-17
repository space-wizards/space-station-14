// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Sith;

namespace Content.Server.DeadSpace.Sith.Components;

[RegisterComponent, Access(typeof(SithSubmissionConditionsSystem))]
public sealed partial class SithSubmissionConditionsComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public List<EntityUid> SubordinateCommand = new List<EntityUid>();

    [ViewVariables(VVAccess.ReadOnly)]
    public float TimeUtilCheckState = 1f;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Progress = 0;
}
