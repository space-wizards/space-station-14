using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Whitelist;
using Content.Shared.GameTicking.Components;

namespace Content.Shared.Trigger.Systems;

public sealed class TriggerOnGameRuleSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GameRuleStartedEvent>(TriggerOnGamerule);
    }
    private void TriggerOnGamerule(ref GameRuleStartedEvent args)
    {
        var query = EntityQueryEnumerator<TriggerOnGameRuleComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (_whitelist.IsWhitelistFail(comp.Whitelist, args.RuleEntity))
                continue;
            if (_whitelist.IsWhitelistPass(comp.Blacklist, args.RuleEntity))
                continue;

            _trigger.Trigger(uid, args.RuleEntity, comp.KeyOut);
        }
    }
}
