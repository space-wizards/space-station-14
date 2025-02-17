// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Sith.Components;

[RegisterComponent]
public sealed partial class SithSubmissionAbilityComponent : Component
{
    [DataField]
    public EntProtoId ActionSithSubmission = "ActionSithSubmission";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionSithSubmissionEntity;

    [ViewVariables(VVAccess.ReadOnly)]
    public float Duration = 4f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxSubmission = 4;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public int Submissions = 0;

}
