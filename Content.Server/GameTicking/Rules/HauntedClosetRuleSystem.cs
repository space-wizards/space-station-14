using Content.Server.GameTicking.Rules.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;

namespace Content.Server.GameTicking.Rules;

public sealed partial class HauntedClosetRuleSystem : GameRuleSystem<HauntedClosetRuleComponent>
{
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void Started(Entity<HauntedClosetRuleComponent, GameRuleComponent> ruleEnt, ref GameRuleStartedEvent args)
    {
        base.Started(ruleEnt, ref args);
        var query = EntityQueryEnumerator<HauntedClosetComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            if (RobustRandom.NextFloat() > comp.CloseChance * ruleEnt.Comp1.ChanceMultiplier)
                continue;

            if (_entityStorage.IsOpen(ent))
            {
                // close it
                _entityStorage.CloseStorage(ent);
            }
            else if (ruleEnt.Comp1.Toggle)
            {
                // open it
                _entityStorage.OpenStorage(ent);
            }
        }
    }
}
