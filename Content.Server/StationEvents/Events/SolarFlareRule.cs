using System.Linq;
using Content.Server.Light.EntitySystems;
using Content.Server.StationEvents.Components;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Radio;
using Content.Shared.Radio.Components;
using Robust.Shared.Random;

namespace Content.Server.StationEvents.Events;

public sealed partial class SolarFlareRule : StationEventSystem<SolarFlareRuleComponent>
{
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedDoorSystem _door = default!;

    private float _effectTimer = 0;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RadioReceiveAttemptEvent>(OnRadioReceiveAttempt);
    }

    protected override void Started(EntityUid uid, SolarFlareRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, comp, gameRule, args);

        for (var i = 0; i < comp.ExtraCount; i++)
        {
            var channel = RobustRandom.Pick(comp.ExtraChannels);
            comp.AffectedChannels.Add(channel);
        }

        comp.AffectedLights = GetEntitiesWithComponentOnStation<PoweredLightComponent>(true).Select(e => (e.Owner, e.Comp)).ToHashSet();
        comp.AffectedAirlocks = GetEntitiesWithComponentOnStation<AirlockComponent>(true).Select(e => (e.Owner, e.Comp)).ToHashSet();
    }

    protected override void ActiveTick(EntityUid uid, SolarFlareRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        base.ActiveTick(uid, component, gameRule, frameTime);

        _effectTimer -= frameTime;
        if (_effectTimer < 0)
        {
            _effectTimer += 1;
            foreach (var light in component.AffectedLights)
            {
                if (RobustRandom.Prob(component.LightBreakChancePerSecond))
                    _poweredLight.TryDestroyBulb(light.Item1, light.Item2);
            }

            foreach (var airlockEnt in component.AffectedAirlocks)
            {
                if (airlockEnt.Item2.AutoClose && RobustRandom.Prob(component.DoorToggleChancePerSecond))
                    _door.TryToggleDoor(airlockEnt.Item1, Comp<DoorComponent>(airlockEnt.Item1));
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

            if (!flare.AffectedChannels.Contains(args.Channel.ID))
                continue;

            if (!flare.OnlyJamHeadsets || (HasComp<HeadsetComponent>(args.RadioReceiver) || HasComp<HeadsetComponent>(args.RadioSource)))
                args.Cancelled = true;
        }
    }
}
