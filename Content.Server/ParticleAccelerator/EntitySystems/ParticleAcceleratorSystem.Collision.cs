using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Server.Singularity.EntitySystems;
using Content.Server.Tesla.Components;
using Content.Server.Tesla.EntitySystems;
using Content.Shared.Singularity.Components;
using Robust.Shared.Physics.Events;

namespace Content.Server.ParticleAccelerator.EntitySystems;

public sealed partial class ParticleAcceleratorSystem
{
    #region Event Handlers
    /// <summary>
    /// Handles PA Particles colliding with a singularity generator or a tesla ball.
    /// Adds power from the particle to the generator or the  tesla ball.
    /// TODO: Desnowflake this.
    /// </summary>
    /// <param name="uid">The uid of the PA particles have collided with.</param>
    /// <param name="component">The state of the PA particles.</param>
    /// <param name="args">The state of the beginning of the collision.</param>
    private void HandleParticleCollide(Entity<ParticleProjectileComponent> entity, ref StartCollideEvent args)
    {
        if (EntityManager.TryGetComponent<SingularityGeneratorComponent>(args.OtherEntity, out var singularityGeneratorComponent))
        {
            // TODO: Unhardcode this.
            EntityManager.System<SingularityGeneratorSystem>().SetPower(
                args.OtherEntity,
                singularityGeneratorComponent.Power + entity.Comp.State switch
                {
                    ParticleAcceleratorPowerState.Standby => 0,
                    ParticleAcceleratorPowerState.Level0 => 1,
                    ParticleAcceleratorPowerState.Level1 => 2,
                    ParticleAcceleratorPowerState.Level2 => 4,
                    ParticleAcceleratorPowerState.Level3 => 8,
                    _ => 0
                },
                singularityGeneratorComponent
            );
            EntityManager.QueueDeleteEntity(entity.Owner);
        }
        else if (TryComp<TeslaEnergyBallComponent>(args.OtherEntity, out var teslaComp))
        {
            // TODO: Unhardcode this.
            // idk what values we want, I just know that it takes 100 energy total to spawn a miniball
            // EmoGarbage might change the PA levels to use actual numbers instead of imaginary ones
            // in regards to rebalancing the singularity, so I might be able to use that instead
            var energyFromParticle = entity.Comp.State switch
            {
                ParticleAcceleratorPowerState.Standby => 0,
                ParticleAcceleratorPowerState.Level0 => 10,
                ParticleAcceleratorPowerState.Level1 => 25,
                ParticleAcceleratorPowerState.Level2 => 50,
                ParticleAcceleratorPowerState.Level3 => 100,
                _ => 0
            };

            if (energyFromParticle == 0)
                if (TryComp<SinguloFoodComponent>(entity, out var singuloFoodComp)) // decelerator moment
                    energyFromParticle = (int) singuloFoodComp.Energy;

            EntityManager.System<TeslaEnergyBallSystem>().AdjustEnergy(args.OtherEntity, teslaComp, energyFromParticle);
            EntityManager.QueueDeleteEntity(entity);
        }
    }
    #endregion Event Handlers
}
