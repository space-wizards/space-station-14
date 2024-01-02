using Content.Server.NodeContainer;
using Content.Shared.CCVar;
using Content.Shared.Revolutionary;
using Content.Shared.Revolutionary.Components;
using Robust.Shared.Configuration;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Revolutionary;

public sealed class RevolutionarySystem : SharedRevolutionarySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    public override void Update(float frameTime)
    {
        var curTime = _gameTiming.CurTime;

        var enumerator = EntityQueryEnumerator<RevolutionaryComponent>();

        while (enumerator.MoveNext(out var uid, out var controller))
        {
            if (controller.NextFlashEscapeTime > curTime)
                continue;

            AttemptToFreeFromBeingFlashed(uid, curTime, controller);
        }
    }

    /// <summary>
    /// Randomly attempts to free someone from being flashed. If successful, they cease being a revolutionary.
    /// If unsuccessful, the next time they attempt to free themselves will be more likely to succeed.
    /// </summary>
    public void AttemptToFreeFromBeingFlashed(EntityUid uid, TimeSpan curTime, RevolutionaryComponent rev)
    {
        var baseChance = _configManager.GetCVar(CCVars.BaseChanceOfFlashWearingOff);
        var incChance = _configManager.GetCVar(CCVars.IncrementChanceOfFlashWearingOff);
        var chance = baseChance + incChance * rev.EscapeAttemptsSoFar;

        if (_robustRandom.Prob(chance))
        {
            RaiseLocalEvent(uid, new FreedFromControlMessage());

            return;
        }

        rev.NextFlashEscapeTime = curTime + TimeSpan.FromSeconds(_configManager.GetCVar(CCVars.TimeBetweenFlashWearOffAttempts));
        rev.EscapeAttemptsSoFar++;
    }

    public void ResetRevFlashedTimer(RevolutionaryComponent rev)
    {
        rev.NextFlashEscapeTime = _timing.CurTime + TimeSpan.FromSeconds(_configManager.GetCVar(CCVars.TimeBeforeFlashCanExpire));
    }
}
