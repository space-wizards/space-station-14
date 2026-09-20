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

/// <summary>
/// Handler for events that cause a solar flare for some amount of time.
/// </summary>
/// <remarks>
/// When a solar flare is active, radio communications are disabled on a random set of channels,
/// doors can randomly open and close, and lights can explode.
/// </remarks>
/// <seealso cref="SolarFlareRuleComponent"/>
public sealed partial class SolarFlareRule : StationEventSystem<SolarFlareRuleComponent>
{
    [Dependency] private PoweredLightSystem _poweredLight = default!;
    [Dependency] private SharedDoorSystem _door = default!;

    [Dependency] private EntityQuery<HeadsetComponent> _headsetQuery;

    private float _effectTimer = 0;

    protected override void Started(Entity<SolarFlareRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        for (var i = 0; i < ent.Comp1.ExtraCount; i++)
        {
            var channel = RobustRandom.Pick(ent.Comp1.ExtraChannels);
            ent.Comp1.AffectedChannels.Add(channel);
        }

        ent.Comp1.AffectedLights = Station.GetEntitiesWithComponentOnStation<PoweredLightComponent>(true)
            .Select(e => (e.Owner, e.Comp))
            .ToHashSet();
        ent.Comp1.AffectedAirlocks = Station.GetEntitiesWithComponentOnStation<AirlockComponent>(true)
            .Select(e => (e.Owner, e.Comp))
            .ToHashSet();
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

    [SubscribeLocalEvent]
    private void OnRadioReceiveAttempt(ref RadioReceiveAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        var query = EntityQueryEnumerator<SolarFlareRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var flare, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive((uid, gameRule)))
                continue;

            if (!flare.AffectedChannels.Contains(args.Channel.ID))
                continue;

            if (!flare.OnlyJamHeadsets || _headsetQuery.HasComp(args.RadioReceiver) || _headsetQuery.HasComp(args.RadioSource))
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
