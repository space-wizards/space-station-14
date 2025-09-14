using JetBrains.Annotations;
using Content.Shared.Stray.Weapons.FireUnderBullet;
using Content.Server.Atmos.Components;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Cargo.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.UserInterface;
using Content.Shared.Actions;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Examine;
using Content.Shared.Throwing;
using Content.Shared.Toggleable;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos;
using Robust.Shared.Timing;
using Robust.Shared.Map;
using Content.Shared.Interaction.Events;
using Content.Shared.Item;
using Content.Shared.Weapons.Ranged.Systems;
using Content.Shared.Weapons.Ranged.Components;

namespace Content.Server.Stray.Weapons.FireUnderBullet;


[UsedImplicitly]
public sealed class FireUnderBulletSystem : SharedFireUnderBulletSystem
{

    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly ExplosionSystem _explosions = default!;
    [Dependency] private readonly InternalsSystem _internals = default!;
    [Dependency] private readonly SharedAudioSystem _audioSys = default!;
    [Dependency] private readonly SharedContainerSystem _containers = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    [Dependency] private readonly IEntityManager _ent = default!;
    [Dependency] private readonly IMapManager _map = default!;

    [Dependency] protected readonly IGameTiming Timing = default!;

    private const float TimerDelay = 0.1f;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<FireUnderBulletComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<GunComponent, GunShotEvent>(OnShoot);
        //SubscribeLocalEvent<FireUnderBulletComponent, DroppedEvent>(OnDropped);
        //SubscribeLocalEvent<FireUnderBulletComponent, GettingPickedUpAttemptEvent>(OnGettingPickedUp);
    }

    private void OnShoot(EntityUid uid, GunComponent component, ref GunShotEvent args){
        foreach(var dat in args.Ammo){
            if(!TryComp(dat.Uid, out FireUnderBulletComponent? comp)){
                return;
            }
            comp.pickedUp = false;
            comp.removeTime = Timing.CurTime+TimeSpan.FromSeconds(0.3f);
            comp.minusTime = Timing.CurTime;
            //_audioSys.PlayPvs(component.RuptureSound, uid);
            comp.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
        }
        //component.removeTime = Timing.CurTime+TimeSpan.FromSeconds(0.3f);
        //_audioSys.PlayPvs(component.RuptureSound, uid);
        //component.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
    }

    //private void OnDropped(EntityUid uid, FireUnderBulletComponent component, ref DroppedEvent args){
    //    component.pickedUp = false;
    //    component.removeTime = Timing.CurTime+component.minusTime;
    //    component.minusTime = Timing.CurTime;
    //    //_audioSys.PlayPvs(component.RuptureSound, uid);
    //    component.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
    //    //component.removeTime = Timing.CurTime+TimeSpan.FromSeconds(0.3f);
    //    //_audioSys.PlayPvs(component.RuptureSound, uid);
    //    //component.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
    //}
    //private void OnGettingPickedUp(EntityUid uid, FireUnderBulletComponent component, ref GettingPickedUpAttemptEvent args){
    //    component.pickedUp = true;
    //    component.removeTime = Timing.CurTime+component.minusTime;
    //    component.minusTime = Timing.CurTime;
    //    //_audioSys.PlayPvs(component.RuptureSound, uid);
    //    component.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
    //    //component.removeTime = Timing.CurTime+TimeSpan.FromSeconds(0.3f);
    //    //_audioSys.PlayPvs(component.RuptureSound, uid);
    //    //component.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
    //}

    private void OnInit(EntityUid uid, FireUnderBulletComponent component, ref ComponentInit args){
        component.removeTime = Timing.CurTime+TimeSpan.FromSeconds(0.3f);
        component.minusTime = Timing.CurTime;
        //_audioSys.PlayPvs(component.RuptureSound, uid);
        component.startTime = Timing.CurTime+TimeSpan.FromSeconds(0.07f);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FireUnderBulletComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if(uid!=null&&comp.startTime<Timing.CurTime){
                if(comp.pickedUp==false){
                    ReleaseGas((uid, comp));
                    //var trans = Transform((EntityUid)uid);
                    //if(trans.GridUid!=null){
                    //    _atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                    //}
                    //comp.minusTime = comp.removeTime - Timing.CurTime;
                    if(comp.removeTime<Timing.CurTime){
                        QueueDel(uid);
                    }
                }
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));
                //_atmosphereSystem.ReactTile((EntityUid)trans.GridUid,trans.Coordinates.ToVector2i(_ent,_map));

            }
            //if(comp.Air.TotalMoles<=0){
            //    Del(uid);
            //}
            //if (comp.Air != null)
            //{
            //    _atmosphereSystem.React(comp.Air, comp);
            //}
        }
    }


    private void ReleaseGas(Entity<FireUnderBulletComponent> comp)
    {
        FireUnderBulletComponent component = (FireUnderBulletComponent)comp;
        var removed = component.releaseGas.Clone(); //0.5f*comp.releaseSpeed;
        removed.Volume *= component.releaseSpeed;
        for(int i = 0; i < Atmospherics.TotalNumberOfGases; i++){
            removed.SetMoles(i, removed.GetMoles(i)*component.releaseSpeed);
        }
        //removed.SetMoles(0, 0.1f*comp.releaseSpeed);//new float[]{0.82f,0 ,0,0.18f,0 ,0 ,0 ,0 ,0 ,0};
        //removed.SetMoles(9, 0.4f*comp.releaseSpeed);
        //var removed = RemoveAirVolume(gasTank, gasTank.Comp.ValveOutputRate * TimerDelay);
        var environment = _atmosphereSystem.GetContainingMixture(component.Owner, false, true);
        removed.Temperature = component.releaseTemp;
        if (environment != null)
        {
            _atmosphereSystem.Merge(environment, removed);
        }
        //var strength = removed.TotalMoles * MathF.Sqrt(removed.Temperature);
        //var dir = _random.NextAngle().ToWorldVec();
        //_throwing.TryThrow(gasTank, dir * strength, strength);
       // if (gasTank.Comp.OutputPressure >= MinimumSoundValvePressure)
        //_audioSys.PlayPvs(gasTank.Comp.RuptureSound, gasTank);
    }

    //public GasMixture RemoveAirVolume(Entity<FireUnderBulletComponent> gasTank, float volume)
    //{
    //    var component = gasTank.Comp;
    //    if (component.Air == null)
    //        return new GasMixture(volume);
//
    //    var molesNeeded = component.OutputPressure / (Atmospherics.R * component.Air.Temperature);
//
    //    var air = RemoveAir(gasTank, molesNeeded);
//
    //    if (air != null)
    //        air.Volume = volume;
    //    else
    //        return new GasMixture(volume);
//
    //    return air;
    //}
    //public GasMixture? RemoveAir(Entity<FireUnderBulletComponent> gasTank, float amount)
    //{
    //    var gas = gasTank.Comp.Air?.Remove(amount);
    //    return gas;
    //}
}
