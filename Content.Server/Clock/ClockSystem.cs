using Content.Server.GameTicking.Events;
using Content.Shared.Clock;
using Content.Shared.Destructible;
using Robust.Server.GameStates;
using Robust.Shared.Random;

namespace Content.Server.Clock;

public sealed class ClockSystem : SharedClockSystem
{
    [Dependency] private readonly PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private readonly IRobustRandom _robustRandom = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<GlobalTimeManagerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ClockComponent, BreakageEventArgs>(OnBreak);
    }

    private void OnRoundStart(RoundStartingEvent ev)
    {
        var manager = Spawn();
        AddComp<GlobalTimeManagerComponent>(manager);
    }

    private void OnMapInit(Entity<GlobalTimeManagerComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.TimeOffset = TimeSpan.FromHours(_robustRandom.NextFloat(0, 24));
        _pvsOverride.AddGlobalOverride(ent);
        Dirty(ent);
    }

    private void OnBreak(Entity<ClockComponent> ent, ref BreakageEventArgs args)
    {
        ent.Comp.StuckTime = GetClockTime(ent);
        Dirty(ent, ent.Comp);
    }
}
