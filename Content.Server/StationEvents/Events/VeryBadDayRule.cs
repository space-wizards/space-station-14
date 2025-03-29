using Content.Server.AlertLevel;
using Content.Server.RoundEnd;
using Content.Server.StationEvents.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Drunk;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.Mind.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class VeryBadDayRule : StationEventSystem<VeryBadDayRuleComponent>
{
    [Dependency] private readonly SharedDrunkSystem _drunkSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevelSystem = default!;

    protected override void Started(EntityUid uid,
        VeryBadDayRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        var query = EntityQueryEnumerator<MindContainerComponent, HumanoidAppearanceComponent>();

        while (query.MoveNext(out var ent, out _, out _))
        {
            _drunkSystem.TryApplyDrunkenness(ent, 1000);
            _damageableSystem.TryChangeDamage(ent,
                new DamageSpecifier(_protoMan.Index<DamageGroupPrototype>("Brute"), _random.Next(5, 50)));
        }
        _alertLevelSystem.SetLevel((EntityUid)station, "red", false, true, true);
    }
}
