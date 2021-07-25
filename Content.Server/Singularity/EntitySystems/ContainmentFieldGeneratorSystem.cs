using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class ContainmentFieldGeneratorSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
            SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);
            SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);
        }

        private void HandleParticleCollide(EntityUid uid, ParticleProjectileComponent component, StartCollideEvent args)
        {
            if (args.OtherFixture.Body.Owner.TryGetComponent<SingularityGeneratorComponent>(out var singularityGeneratorComponent))
            {
                singularityGeneratorComponent.Power += component.State switch
                {
                    ParticleAcceleratorPowerState.Standby => 0,
                    ParticleAcceleratorPowerState.Level0 => 1,
                    ParticleAcceleratorPowerState.Level1 => 2,
                    ParticleAcceleratorPowerState.Level2 => 4,
                    ParticleAcceleratorPowerState.Level3 => 8,
                    _ => 0
                };

                EntityManager.QueueDeleteEntity(uid);
            }
        }

        private void HandleGeneratorCollide(EntityUid uid, ContainmentFieldGeneratorComponent component, StartCollideEvent args)
        {
            if (args.OtherFixture.Body.Owner.HasTag("EmitterBolt")) {
                component.ReceivePower(6);
            }
        }

        private void HandleFieldCollide(EntityUid uid, ContainmentFieldComponent component, StartCollideEvent args)
        {
            if (component.Parent == null)
            {
                EntityManager.QueueDeleteEntity(uid);
                return;
            }

            component.Parent.TryRepell(component.Owner, args.OtherFixture.Body.Owner);
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ContainmentFieldGeneratorComponent component,
            PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchoredChanged();
        }
    }
}
