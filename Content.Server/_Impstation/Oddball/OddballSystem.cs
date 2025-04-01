using Content.Server.GameTicking;
using Content.Server.Points;
using Content.Shared.GameTicking.Components;
using Content.Shared.Hands;
using Content.Shared.Points;
using Robust.Shared.Timing;

namespace Content.Server._Impstation.Oddball;

public sealed class OddballSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly PointSystem _point = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OddballComponent, GotEquippedHandEvent>(OnEquipped);
        SubscribeLocalEvent<OddballComponent, GotUnequippedHandEvent>(OnUnequipped);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<OddballComponent>();

        while (query.MoveNext(out _, out var comp))
        {
            if (!comp.Active ||
                comp.Holder is not { } holder ||
                _timing.CurTime < comp.NextUpdate)
                continue;

            comp.NextUpdate += comp.Interval;
            var ruleQuery = EntityQueryEnumerator<OddballRuleComponent, PointManagerComponent, GameRuleComponent>();

            while (ruleQuery.MoveNext(out var ruleUid, out _, out var point, out var rule))
            {
                if (!_gameTicker.IsGameRuleActive(ruleUid, rule))
                    continue;

                _point.AdjustPointValue(holder, comp.PointValue, ruleUid, point);
            }
        }
    }

    private void OnEquipped(Entity<OddballComponent> ent, ref GotEquippedHandEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Active = true;
        ent.Comp.Holder = args.User;
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Interval;
    }

    private void OnUnequipped(Entity<OddballComponent> ent, ref GotUnequippedHandEvent args)
    {
        if (args.Handled)
            return;

        ent.Comp.Active = false;
        ent.Comp.Holder = null;
        ent.Comp.NextUpdate = _timing.CurTime + ent.Comp.Interval;
    }
}
