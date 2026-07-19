using Content.Server.Tesla.Components;
using Content.Server.Lightning;
using Content.Shared.Power;
using Robust.Shared.Timing;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// The component changes the visual of an object after it is struck by lightning
/// </summary>
public sealed partial class LightningSparkingSystem : EntitySystem
{
    private static readonly EntityTimerId SparkingTimer = new("sparking");

    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private IGameTiming _gameTiming = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LightningSparkingComponent, HitByLightningEvent>(OnHitByLightning);
        SubscribeLocalEvent<LightningSparkingComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnHitByLightning(Entity<LightningSparkingComponent> uid, ref HitByLightningEvent args)
    {
        _appearance.SetData(uid.Owner, TeslaCoilVisuals.Lightning, true);
        uid.Comp.LightningEndTime = _gameTiming.CurTime + TimeSpan.FromSeconds(uid.Comp.LightningTime);
        uid.Comp.IsSparking = true;
        _timers.SetTimerAt(uid, SparkingTimer, uid.Comp.LightningEndTime);
    }

    private void OnTimer(Entity<LightningSparkingComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != SparkingTimer || !ent.Comp.IsSparking)
            return;

        _appearance.SetData(ent, TeslaCoilVisuals.Lightning, false);
        ent.Comp.IsSparking = false;
    }
}
