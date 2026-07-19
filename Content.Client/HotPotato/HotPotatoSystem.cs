using Content.Shared.HotPotato;
using Robust.Shared.Random;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.HotPotato;

public sealed partial class HotPotatoSystem : SharedHotPotatoSystem
{
    private static readonly EntityTimerId EffectTimer = new("effect");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    private readonly EntProtoId _hotPotatoEffectId = "HotPotatoEffect";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveHotPotatoComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ActiveHotPotatoComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnStartup(Entity<ActiveHotPotatoComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.TargetTime < _timing.CurTime)
            ent.Comp.TargetTime = _timing.CurTime;
        Schedule(ent);
    }

    private void OnTimer(Entity<ActiveHotPotatoComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != EffectTimer)
            return;

        ent.Comp.TargetTime = args.FiredAt + TimeSpan.FromSeconds(ent.Comp.EffectCooldown);
        Schedule(ent);

        if (_timing.IsFirstTimePredicted)
            Spawn(_hotPotatoEffectId, _transform.GetMapCoordinates(ent).Offset(_random.NextVector2(0.25f)));
    }

    private void Schedule(Entity<ActiveHotPotatoComponent> ent)
    {
        _timers.SetTimerAt(ent, EffectTimer, ent.Comp.TargetTime);
    }
}
