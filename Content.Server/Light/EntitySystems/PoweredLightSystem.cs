using Content.Server.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems;

/// <summary>
///     System for the PoweredLightComponents
/// </summary>
public sealed class PoweredLightSystem : SharedPoweredLightSystem
{
    /// <summary>
    /// The cost, in boo budget points, of flickering a light.
    /// </summary>
    const int FlickerBooCost = 4;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);
    }

    private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
    {
        if (light.IgnoreGhostsBoo || HasComp<BlinkingPoweredLightComponent>(uid))
            return; // The light is immune or already blinking.

        // check cooldown first to prevent abuse
        var curTime = GameTiming.CurTime;
        if (light.LastGhostBlink != null && curTime <= light.LastGhostBlink + light.GhostBlinkingCooldown)
            return;

        light.LastGhostBlink = curTime;

        var blinkingComp = EnsureComp<BlinkingPoweredLightComponent>(uid);
        blinkingComp.StopBlinkingTime = curTime + light.GhostBlinkingTime;
        Dirty(uid, blinkingComp);

        args.Cost = FlickerBooCost;
        args.Handled = true;
    }

    private void OnMapInit(EntityUid uid, PoweredLightComponent light, MapInitEvent args)
    {
        // TODO: Use ContainerFill dog
        if (light.HasLampOnSpawn != null)
        {
            var entity = Spawn(light.HasLampOnSpawn, Comp<TransformComponent>(uid).Coordinates);
            ContainerSystem.Insert(entity, light.LightBulbContainer);
        }
        // need this to update visualizers
        UpdateLight(uid, light);
    }
}
