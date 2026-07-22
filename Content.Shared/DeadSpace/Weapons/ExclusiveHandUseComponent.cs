// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Weapons;

/// <summary>
/// Prevents the holder from attacking with another held weapon.
/// </summary>
[RegisterComponent]
public sealed partial class ExclusiveHandUseComponent : Component
{
    [DataField]
    public LocId Popup = "exclusive-hand-use-blocked";

    [DataField]
    public HashSet<EntProtoId> BlockedItems = [];

    /// <summary>
    /// When non-empty, only these item prototypes may be used as ranged or melee weapons.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> AllowedItems = [];
}
