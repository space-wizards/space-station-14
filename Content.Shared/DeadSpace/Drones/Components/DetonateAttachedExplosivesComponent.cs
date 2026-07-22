// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Actions;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Drones.Components;

[RegisterComponent]
public sealed partial class DetonateAttachedExplosivesComponent : Component
{
    [DataField]
    public EntityWhitelist? ExplosiveWhitelist;

    [DataField]
    public EntProtoId Action = "ToyCarDetonateAttachedExplosives";

    public EntityUid? ActionEntity;
}

public sealed partial class DetonateAttachedExplosivesActionEvent : InstantActionEvent;
