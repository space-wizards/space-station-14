using Content.Server.GameTicking.Rules.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Lock;

namespace Content.Server.GameTicking.Rules;

public sealed partial class HauntedClosetRuleSystem : GameRuleSystem<HauntedClosetRuleComponent>
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private LockSystem _lockSystem = default!;

    protected override void Started(Entity<HauntedClosetRuleComponent, GameRuleComponent> ruleEnt, ref GameRuleStartedEvent args)
    {
        base.Started(ruleEnt, ref args);

        var query = EntityQueryEnumerator<HauntedClosetComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (RobustRandom.NextFloat() > comp.CloseChance * ruleEnt.Comp1.ChanceMultiplier)
                continue;

            Haunt(ent, comp, ruleEnt.Comp1);
        }
    }

    private void Haunt(EntityUid ent, HauntedClosetComponent haunted, HauntedClosetRuleComponent rule)
    {
        if (_entityStorage.IsOpen(ent))
        {
            // close it
            _entityStorage.CloseStorage(ent);
        }
        else if (rule.Toggle)
        {
            // open it
            _lockSystem.Unlock(ent, null);
            _entityStorage.OpenStorage(ent);
        }
    }
}
