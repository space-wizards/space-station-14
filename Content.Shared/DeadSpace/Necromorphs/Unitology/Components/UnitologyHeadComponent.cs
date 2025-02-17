// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Robust.Shared.GameStates;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.DeadSpace.Necromorphs.Unitology.Components;

/// <summary>
/// Used for marking regular unitologs as well as storing icon prototypes so you can see fellow unitologs.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedUnitologySystem))]
public sealed partial class UnitologyHeadComponent : Component
{
    [DataField("actionUnitologyHead", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionUnitologyHead = "ActionUnitologyHead";

    [DataField("actionAbsorptionDeadNecroEntity")]
    public EntityUid? ActionUnitologyHeadEntity;

    [DataField("actionOrderToSlave", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>))]
    public string ActionOrderToSlave = "ActionOrderToSlave";

    [DataField("actionOrderToSlaveEntity")]
    public EntityUid? ActionOrderToSlaveEntity;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype> StatusIcon { get; set; } = "UnitologyHeadFaction";

    public override bool SessionSpecific => true;
}
