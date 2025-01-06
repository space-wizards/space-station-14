using Content.Server.AlertLevel;
using Content.Shared.Station.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization;

namespace Content.Server.Weapons.Ranged.Conditions;

public sealed partial class AlertLevelCondition : FireModeCondition
{
    [DataField(required: true)]
    public string AlertLevel;
    
    public override bool Condition(FireModeConditionConditionArgs args)
    { 
        var ent = args.EntityManager;
        
        var _alert = ent.System<AlertLevelSystem>();
    
        if (!args.EntityManager.TryGetComponent<TransformComponent>(args.Shooter, out var transformComp))
            return false;
        
        if (!ent.TryGetComponent<StationMemberComponent>(transformComp.ParentUid, out var stationMember) 
            || !ent.TryGetComponent<AlertLevelComponent>(stationMember.Station, out var alertLevelComp) 
            || _alert.GetLevel(stationMember.Station, alertLevelComp) != AlertLevel)
            return false;
            
        return true;
    }
}