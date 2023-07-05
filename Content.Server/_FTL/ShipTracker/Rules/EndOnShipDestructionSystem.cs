using System.Linq;
using Content.Server._FTL.ShipHealth;
using Content.Server._FTL.ShipTracker.Rules.Components;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.RoundEnd;

namespace Content.Server._FTL.ShipTracker.Rules;

/// <summary>
/// Manages <see cref="EndOnShipDestructionComponent"/>
/// </summary>
public sealed class EndOnShipDestructionSystem : GameRuleSystem<EndOnShipDestructionComponent>
{
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundEndTextAppendEvent>(OnRoundEndText);
    }

    private void OnRoundEndText(RoundEndTextAppendEvent ev)
    {
        ev.AddLine(Loc.GetString("ftl-gamerule-end-text"));
    }

    protected override void ActiveTick(EntityUid uid, EndOnShipDestructionComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);
        var query = EntityQuery<MainCharacterShipComponent>().ToList().Count;
        if (query > 0) // main character ship still exists
            return;

        _roundEndSystem.EndRound();
    }
}
