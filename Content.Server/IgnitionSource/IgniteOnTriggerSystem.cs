using Content.Server.Explosion.EntitySystems;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
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

            _source.SetIgnited((uid, source), false);
        }
    }

    private void OnTrigger(Entity<IgniteOnTriggerComponent> ent, ref TriggerEvent args)
    {
        // prevent spamming sound and ignition
        if (!TryComp(ent.Owner, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((ent.Owner, useDelay)))
            return;

        _source.SetIgnited(ent.Owner);
        _audio.PlayPvs(ent.Comp.IgniteSound, ent);

        _useDelay.TryResetDelay((ent.Owner, useDelay));
        ent.Comp.IgnitedUntil = _timing.CurTime + ent.Comp.IgnitedTime;
    }
}
