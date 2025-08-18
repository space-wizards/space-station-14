using Content.Server.AlertLevel;
using Content.Server.Popups;
using Content.Shared.Station.Components;
using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared._Starlight.Weapons.Ranged.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Set;
using Robust.Shared.Serialization;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Weapons.Ranged.Conditions;

public sealed partial class AlertLevelCondition : FireModeCondition
{
    [DataField(required: true)]
    public List<string> AlertLevels;

    [DataField]
    public override string PopupMessage { get; set; } = "firemode-alert-level-condition";

    public override bool Condition(FireModeConditionConditionArgs args)
    {
        var entityManager = args.EntityManager;

        var alertSystem = entityManager.System<AlertLevelSystem>();

        var _popupSystem = entityManager.System<PopupSystem>();

        if (!entityManager.TryGetComponent<TransformComponent>(args.Shooter, out var transformComp)
            || !entityManager.TryGetComponent<ActorComponent>(args.Shooter, out var actor))
            return false;

        if (entityManager.TryGetComponent<StationMemberComponent>(transformComp.ParentUid, out var stationMember) &&
            entityManager.TryGetComponent<AlertLevelComponent>(stationMember.Station, out var alertLevelComp))
        {
            var currentAlertLevel = alertSystem.GetLevel(stationMember.Station, alertLevelComp);
            if (!AlertLevels.Contains(currentAlertLevel))
                _popupSystem.PopupEntity(Loc.GetString(PopupMessage), args.Shooter, actor.PlayerSession);
            return AlertLevels.Contains(currentAlertLevel);
        }

        _popupSystem.PopupEntity(Loc.GetString(PopupMessage), args.Shooter, actor.PlayerSession);
        return false;
    }
}