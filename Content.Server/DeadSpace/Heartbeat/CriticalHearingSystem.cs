using Content.Server.Radio;
using Content.Shared.DeadSpace.Heartbeat;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Server.DeadSpace.Heartbeat;

public sealed class CriticalHearingSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        var listener = args.RadioReceiver;

        for (var i = 0; i < 4; i++)
        {
            if (HasComp<CritHeartbeatComponent>(listener) &&
                TryComp<MobStateComponent>(listener, out var mobState) &&
                mobState.CurrentState is MobState.PreCritical or MobState.Critical)
            {
                args.Cancelled = true;
                return;
            }

#pragma warning disable RA0030 // The receiver can be EntityUid.Invalid; the UID transform helper would throw.
            if (!TryComp<TransformComponent>(listener, out var transform))
#pragma warning restore RA0030
                return;

            var parent = transform.ParentUid;
            if (parent == listener)
                return;

            listener = parent;
        }
    }
}
