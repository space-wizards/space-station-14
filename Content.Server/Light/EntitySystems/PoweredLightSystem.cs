using Content.Server.Ghost;
using Content.Shared.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems;

/// <inheritdoc/>
public sealed class PoweredLightSystem : SharedPoweredLightSystem
{
    [SubscribeLocalEvent]
    private void OnGhostBoo(Entity<PoweredLightComponent> ent, ref GhostBooEvent args)
    {
        // Already handled
        if (args.AllowedIntensity < GhostBooIntensity.Normal
            || args.ResponseIntensity != GhostBooIntensity.None)
            return;

        if (ent.Comp.IgnoreGhostsBoo || HasComp<BlinkingPoweredLightComponent>(ent))
            return; // The light is immune or already blinking.

        // check cooldown first to prevent abuse
        var curTime = GameTiming.CurTime;
        if (ent.Comp.LastGhostBlink != null && curTime <= ent.Comp.LastGhostBlink + ent.Comp.GhostBlinkingCooldown)
            return;

        ent.Comp.LastGhostBlink = curTime;

        var blinkingComp = EnsureComp<BlinkingPoweredLightComponent>(ent);
        blinkingComp.StopBlinkingTime = curTime + ent.Comp.GhostBlinkingTime;
        Dirty(ent, blinkingComp);

        args.ResponseIntensity = GhostBooIntensity.Normal;
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<PoweredLightComponent> ent, ref MapInitEvent args)
    {
        // TODO: Use ContainerFill dog
        if (ent.Comp.HasLampOnSpawn != null)
        {
            var entity = Spawn(ent.Comp.HasLampOnSpawn, Transform(ent).Coordinates);
            ContainerSystem.Insert(entity, ent.Comp.LightBulbContainer);
        }
        // need this to update visualizers
        UpdateLight(ent, ent.Comp);
    }
}
