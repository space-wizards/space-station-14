using Content.Server.Explosion.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Audio;
using Robust.Shared.Timing;

namespace Content.Server.IgnitionSource;

/// <summary>
/// Handles igniting when triggered and stopping ignition after the delay.
/// </summary>
public sealed class IgniteOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IgnitionSourceSystem _source = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<IgniteOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);

        var query = EntityQueryEnumerator<IgniteOnTriggerComponent, IgnitionSourceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var source))
        {
            if (!source.Ignited)
                continue;

            if (_timing.CurTime < comp.IgnitedUntil)
                continue;

            _source.SetIgnited(uid, false, source);
        }
    }

    private void OnTrigger(EntityUid uid, IgniteOnTriggerComponent comp, TriggerEvent args)
    {
        // prevent spamming sound and ignition
        TryComp<UseDelayComponent>(uid, out var delay);
        if (_useDelay.ActiveDelay(uid, delay))
            return;

        _source.SetIgnited(uid);
        _audio.PlayPvs(comp.IgniteSound, uid);

        _useDelay.BeginDelay(uid, delay);
        comp.IgnitedUntil = _timing.CurTime + comp.IgnitedTime;
    }
}
