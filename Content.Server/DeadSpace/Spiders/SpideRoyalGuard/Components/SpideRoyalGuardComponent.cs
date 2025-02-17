// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Server.DeadSpace.Spiders.SpideRoyalGuard.Components;

[RegisterComponent]
public sealed partial class SpideRoyalGuardComponent : Component
{

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public bool IsGuards = false;
    public TimeSpan TileLeftCheckKing = TimeSpan.Zero;

    [DataField, ViewVariables(VVAccess.ReadOnly)]
    public float Range = 8f;

    [DataField]
    public ProtoId<AlertPrototype> SpiderGuardAlert = "SpiderGuard";

}
