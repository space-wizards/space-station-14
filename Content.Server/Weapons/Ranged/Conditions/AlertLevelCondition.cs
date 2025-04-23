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
    public List<string> AlertLevels;

    public override bool Condition(FireModeConditionConditionArgs args)
    {
        var entityManager = args.EntityManager;

        var alertSystem = entityManager.System<AlertLevelSystem>();

        if (!entityManager.TryGetComponent<TransformComponent>(args.Shooter, out var transformComp))
            return false;

        if (entityManager.TryGetComponent<StationMemberComponent>(transformComp.ParentUid, out var stationMember) &&
            entityManager.TryGetComponent<AlertLevelComponent>(stationMember.Station, out var alertLevelComp))
        {
            var currentAlertLevel = alertSystem.GetLevel(stationMember.Station, alertLevelComp);
            return AlertLevels.Contains(currentAlertLevel);
        }

        return false;
    }
}