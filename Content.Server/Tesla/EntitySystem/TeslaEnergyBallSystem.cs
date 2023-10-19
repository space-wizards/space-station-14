using Content.Server.Singularity.Components;
using Content.Shared.Singularity.EntitySystems;
using Content.Server.Tesla.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.Tesla.EntitySystems;

/// <summary>
/// A component that takes energy and spends it to spawn mini energy balls
/// </summary>
public sealed class TeslaEnergyBallSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TeslaEnergyBallComponent, StartCollideEvent>(HandleParticleCollide);
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

    private void HandleParticleCollide(EntityUid uid, TeslaEnergyBallComponent component, ref StartCollideEvent args)
    {
        if (!TryComp<SinguloFoodComponent>(args.OtherEntity, out var singuloFood))
            return;

        AdjustEnergy(uid, component, singuloFood.Energy);
        EntityManager.QueueDeleteEntity(args.OtherEntity);

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
