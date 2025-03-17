using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Map;

namespace Content.Server.Heretic.Components;

[RegisterComponent]
[AutoGenerateComponentPause]
public sealed partial class HellVictimComponent : Component
{
    [DataField]
    public bool AlreadyHelled = false;

    [DataField]
    public bool CleanupDone = false;

    [ViewVariables]
    [AutoPausedField]
    public TimeSpan ExitHellTime = default!;

    [DataField]
    public EntityUid OriginalBody;

    [ViewVariables(VVAccess.ReadOnly)]
    public EntityCoordinates OriginalPosition;

    [DataField]
    public SpeciesPrototype? CloneProto;

    [DataField]
    public EntityUid Mind;

    [DataField, AutoNetworkedField]
    public TimeSpan HellDuration = TimeSpan.FromSeconds(15);

}
