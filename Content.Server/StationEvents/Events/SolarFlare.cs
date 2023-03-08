using Content.Server.GameTicking.Rules.Configurations;
using Content.Server.Radio.Components;
using Content.Server.Radio;
using Robust.Shared.Random;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Components;
using Content.Shared.Radio.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;

namespace Content.Server.StationEvents.Events;

public sealed class SolarFlare : StationEventSystem
{
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;

    public override string Prototype => "SolarFlare";

    private SolarFlareEventRuleConfiguration _event = default!;
    private float _effectTimer = 0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ActiveRadioComponent, RadioReceiveAttemptEvent>(OnRadioSendAttempt);
    }

    public override void Added()
    {
        base.Added();

        if (Configuration is not SolarFlareEventRuleConfiguration ev)
            return;

        _event = ev;
        _event.EndAfter = RobustRandom.Next(ev.MinEndAfter, ev.MaxEndAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!RuleStarted)
            return;

        _effectTimer -= frameTime;
        if (_effectTimer < 0)
        {
            _effectTimer += 1;
            foreach (var comp in EntityQuery<PoweredLightComponent>())
            {
                if (RobustRandom.Prob(_event.LightBreakChancePerSecond))
                    _poweredLight.TryDestroyBulb(comp.Owner, comp);
            }

            foreach (var comp in EntityQuery<DoorComponent>())
            {
                if (RobustRandom.Prob(_event.DoorToggleChancePerSecond))
                    _door.TryToggleDoor(comp.Owner, comp);
            }
        }

        if (Elapsed > _event.EndAfter)
        {
            ForceEndSelf();
            return;
        }
    }

    private void OnRadioSendAttempt(EntityUid uid, ActiveRadioComponent component, RadioReceiveAttemptEvent args)
    {
        if (RuleStarted && _event.AffectedChannels.Contains(args.Channel.ID))
            if (!_event.OnlyJamHeadsets || (HasComp<HeadsetComponent>(uid) || HasComp<HeadsetComponent>(args.RadioSource)))
                args.Cancel();
    }
}
