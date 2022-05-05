using Content.Server.Explosion.EntitySystems;
using Content.Server.Power.Components;

namespace Content.Server.PowerSink
{
    public sealed class PowerSinkSystem : EntitySystem
    {
        [Dependency] private readonly ExplosionSystem _explosionSystem = default!;

        public override void Update(float frameTime)
        {
            foreach (var comp in EntityManager.EntityQuery<PowerSinkComponent>())
            {
                if (Comp<TransformComponent>(comp.Owner).Anchored)
                {
                    var networkLoad = Comp<PowerConsumerComponent>(comp.Owner).NetworkLoad;
                    // Charge rate is multiplied by how much power it can get
                    comp.Charge += networkLoad.ReceivingPower / networkLoad.DesiredPower * frameTime;
                    if (comp.Charge >= comp.Capacity)
                    {
                        _explosionSystem.QueueExplosion(comp.Owner, "Default", 5, 1, 5, canCreateVacuum: false);
                        comp.AlreadyExploded = true;
                    }
                }
            }
        }
    }
}
