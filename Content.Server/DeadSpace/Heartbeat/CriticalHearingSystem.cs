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

            var parent = Transform(listener).ParentUid;
            if (parent == listener)
                return;

            listener = parent;
        }
    }
}
