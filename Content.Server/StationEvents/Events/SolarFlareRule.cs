using Content.Server.GameTicking.Rules.Components;
using Content.Server.Radio;
using Content.Server.Light.EntitySystems;
using Content.Server.Light.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed class SolarFlareRule : StationEventSystem<SolarFlareRuleComponent>
{
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedDoorSystem _door = default!;

    private float _effectTimer = 0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    protected override void Started(EntityUid uid, SolarFlareRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        foreach (var id in comp.AffectedChannels)
        {
            var channel = _proto.Index<RadioChannelPrototype>(id);
            comp.AffectedFrequencies.Add(channel.Frequency);
        }

        for (var i = 0; i < comp.ExtraCount; i++)
        {
            var id = RobustRandom.Pick(comp.ExtraChannels);
            var channel = _proto.Index<RadioChannelPrototype>(id);
            comp.AffectedFrequencies.Add(channel.Frequency);
        }
    }

    protected override void ActiveTick(EntityUid uid, SolarFlareRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        _effectTimer -= frameTime;
        if (_effectTimer < 0)
        {
            _effectTimer += 1;
            var lightQuery = EntityQueryEnumerator<PoweredLightComponent>();
            while (lightQuery.MoveNext(out var lightEnt, out var light))
            {
                if (RobustRandom.Prob(component.LightBreakChancePerSecond))
                    _poweredLight.TryDestroyBulb(lightEnt, light);
            }
            var airlockQuery = EntityQueryEnumerator<AirlockComponent, DoorComponent>();
            while (airlockQuery.MoveNext(out var airlockEnt, out var airlock, out var door))
            {
                if (airlock.AutoClose && RobustRandom.Prob(component.DoorToggleChancePerSecond))
                    _door.TryToggleDoor(airlockEnt, door);
            }
        }
    }

    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        var query = EntityQueryEnumerator<SolarFlareRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var flare, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                continue;

            if (!flare.AffectedFrequencies.Contains(args.Channel.Frequency))
                continue;

            if (!flare.OnlyJamHeadsets || (HasComp<HeadsetComponent>(args.RadioReceiver) || HasComp<HeadsetComponent>(args.RadioSource)))
                args.Cancelled = true;
        }
    }
}
