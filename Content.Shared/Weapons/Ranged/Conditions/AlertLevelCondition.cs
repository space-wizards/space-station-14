using Content.Shared.AlertLevel;
using Content.Shared.Station.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;

namespace Content.Shared.Weapons.Ranged.Conditions;

[Serializable, NetSerializable]
public sealed partial class AlertLevelCondition : FireModeCondition
{
    [DataField(required: true)]
    public string AlertLevel;
    
    public override bool Condition(FireModeConditionConditionArgs args)
    { 
        if (!args.EntityManager.TryGetComponent<TransformComponent>(args.Shooter, out var transformComp))
            return false;
        
        if (!args.EntityManager.TryGetComponent<StationMemberComponent>(transformComp.ParentUid, out var stationMember) 
            || !args.EntityManager.TryGetComponent<AlertLevelComponent>(stationMember.Station, out var alertLevelComp) 
            || alertLevelComp.CurrentLevel != AlertLevel)
            return false;
            
        return true;
    }
}