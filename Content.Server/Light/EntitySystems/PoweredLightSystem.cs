using Content.Server.Administration.Logs;
using Content.Server.AlertLevel;
using Content.Server.DeviceLinking.Systems;
using Content.Server.DeviceNetwork;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.Emp;
using Content.Server.Ghost;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;

namespace Content.Server.Light.EntitySystems;

/// <summary>
///     System for the PoweredLightComponents
/// </summary>
public sealed class PoweredLightSystem : SharedPoweredLightSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PoweredLightComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<PoweredLightComponent, GhostBooEvent>(OnGhostBoo);

        SubscribeLocalEvent<PoweredLightComponent, EmpPulseEvent>(OnEmpPulse);
        SubscribeLocalEvent<AlertLevelChangedEvent>(OnAlertLevelChanged); //Starlight
    }

    private void OnGhostBoo(EntityUid uid, PoweredLightComponent light, GhostBooEvent args)
    {
        if (light.IgnoreGhostsBoo)
            return;

        // check cooldown first to prevent abuse
        var time = GameTiming.CurTime;
        if (light.LastGhostBlink != null)
        {
            if (time <= light.LastGhostBlink + light.GhostBlinkingCooldown)
                return;
        }

        light.LastGhostBlink = time;

        ToggleBlinkingLight(uid, light, true);
        uid.SpawnTimer(light.GhostBlinkingTime, () =>
        {
            ToggleBlinkingLight(uid, light, false);
        });

        args.Handled = true;
    }

    private void OnMapInit(EntityUid uid, PoweredLightComponent light, MapInitEvent args)
    {
        // TODO: Use ContainerFill dog
        if (light.HasLampOnSpawn != null)
        {
            var entity = EntityManager.SpawnEntity(light.HasLampOnSpawn, EntityManager.GetComponent<TransformComponent>(uid).Coordinates);
            ContainerSystem.Insert(entity, light.LightBulbContainer);
        }
        // need this to update visualizers
        UpdateLight(uid, light);
    }

    private void OnEmpPulse(EntityUid uid, PoweredLightComponent component, ref EmpPulseEvent args)
    {
        if (TryDestroyBulb(uid, component))
            args.Affected = true;
    }
    
    #region Starlight
    private void OnAlertLevelChanged(AlertLevelChangedEvent args)
    {
        var query = EntityQueryEnumerator<PoweredLightComponent>();
        while (query.MoveNext(out var uid, out var light))
        {
            if (args.AlertLevel == "delta" || args.AlertLevel == "epsilon")
                SetState(uid, false, light);
            else
                SetState(uid, true, light);
        }
    }
    #endregion Starlight
}
