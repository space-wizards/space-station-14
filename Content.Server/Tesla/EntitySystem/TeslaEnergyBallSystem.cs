using Content.Server.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Server.Tesla.Components;
using Robust.Shared.Physics.Events;
using Content.Shared.Mobs.Components;
using Microsoft.Extensions.DependencyModel;
using Content.Server.Physics.Controllers;
using Content.Server.Lightning.Components;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// A component that takes energy and spends it to spawn mini energy balls.
/// </summary>
public sealed class TeslaEnergyBallSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaEnergyBallComponent, StartCollideEvent>(OnStartCollide);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeslaEnergyBallComponent>();
        while (query.MoveNext(out var uid, out var teslaEnergyBall))
        {
            teslaEnergyBall.AccumulatedFrametime += frameTime;

            if (teslaEnergyBall.AccumulatedFrametime < teslaEnergyBall.UpdateInterval)
                continue;

            AdjustEnergy(uid, teslaEnergyBall, -teslaEnergyBall.EnergyLoss * teslaEnergyBall.AccumulatedFrametime);
            Log.Debug("Текущая энергия: " + teslaEnergyBall.Energy);
            teslaEnergyBall.AccumulatedFrametime = 0f;
        }
    }

    private void OnStartCollide(EntityUid uid, TeslaEnergyBallComponent component, ref StartCollideEvent args)
    {
        if (TryComp<SinguloFoodComponent>(args.OtherEntity, out var singuloFood))
        {
            AdjustEnergy(uid, component, singuloFood.Energy);
            EntityManager.QueueDeleteEntity(args.OtherEntity);
        }
        if (TryComp<LightningTargetComponent>(args.OtherEntity, out var target))
        {
            //Sound here
            //Effect here
            EntityManager.QueueDeleteEntity(args.OtherEntity);
            AdjustEnergy(uid, component, 50f);
        }
    }
    public void AdjustEnergy(EntityUid uid, TeslaEnergyBallComponent component, float delta)
    {
        component.Energy += delta;

        if (component.Energy > component.NeedEnergyToSpawn) {
            component.Energy -= component.NeedEnergyToSpawn;
            Spawn(component.SpawnProto, Transform(uid).Coordinates);
        }
        if (component.Energy < component.EnergyToDespawn)
        {
            QueueDel(uid);
        }
    }
}
