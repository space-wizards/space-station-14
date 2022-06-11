using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Construction.Components;
using Content.Shared.Interaction;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class ContainmentFieldGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tags = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, PhysicsBodyTypeChangedEvent>(BodyTypeChanged);
            SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);
            SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);
            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, UnanchorAttemptEvent>(OnUnanchorAttempt);
            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, InteractHandEvent>(OnInteract);
        }

        private void OnInteract(EntityUid uid, ContainmentFieldGeneratorComponent component, InteractHandEvent args)
        {
            if (TryComp(component.Owner, out TransformComponent? transformComp) && transformComp.Anchored)
            {
                if (!component.Enabled)
                    TurnOn(component);
                else if (component.Enabled && component.IsConnected)
                {
                    return;
                }
                else
                    TurnOff(component);
            }
            args.Handled = true;
        }

        private void TurnOn(ContainmentFieldGeneratorComponent component)
        {
            component.Enabled = true;
        }

        private void TurnOff(ContainmentFieldGeneratorComponent component)
        {
            component.Enabled = false;
        }

        private void OnUnanchorAttempt(EntityUid uid, ContainmentFieldGeneratorComponent component, UnanchorAttemptEvent args)
        {
            if (component.Enabled)
            {
                args.Cancel();
            }
        }

        private void HandleParticleCollide(EntityUid uid, ParticleProjectileComponent component, StartCollideEvent args)
        {
            if (EntityManager.TryGetComponent<SingularityGeneratorComponent?>(args.OtherFixture.Body.Owner, out var singularityGeneratorComponent))
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
            if (_tags.HasTag(args.OtherFixture.Body.Owner, "EmitterBolt")) {
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
        }

        private static void BodyTypeChanged(
            EntityUid uid,
            ContainmentFieldGeneratorComponent component,
            ref PhysicsBodyTypeChangedEvent args)
        {
            component.OnAnchoredChanged();
        }
    }
}
