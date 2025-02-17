// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Shared.DeadSpace.Necromorphs.Unitology.Components;

/// <summary>
/// Used for marking regular unitologs as well as storing icon prototypes so you can see fellow unitologs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedUnitologySystem))]
public sealed partial class UnitologyComponent : Component
{
    /// <summary>
    /// The status icon prototype displayed for unitologs
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "UnitologyFaction";
    
    public override bool SessionSpecific => true;
}
