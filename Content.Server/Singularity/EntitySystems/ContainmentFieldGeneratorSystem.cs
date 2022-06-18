using Content.Server.ParticleAccelerator.Components;
using Content.Server.Singularity.Components;
using Content.Shared.Physics;
using Content.Shared.Singularity.Components;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Server.Singularity.EntitySystems
{
    public sealed class ContainmentFieldGeneratorSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tags = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, ComponentRemove>(OnComponentRemoved);
            SubscribeLocalEvent<ContainmentFieldComponent, StartCollideEvent>(HandleFieldCollide);
            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, StartCollideEvent>(HandleGeneratorCollide);
            SubscribeLocalEvent<ParticleProjectileComponent, StartCollideEvent>(HandleParticleCollide);
            SubscribeLocalEvent<ContainmentFieldGeneratorComponent, AnchorStateChangedEvent>(OnAnchorChanged);
        }

        private void OnComponentRemoved(EntityUid uid, ContainmentFieldGeneratorComponent component, ComponentRemove args)
        {
            component.Connection1?.Item2.Dispose();
            component.Connection2?.Item2.Dispose();
        }

        private void OnAnchorChanged(EntityUid uid, ContainmentFieldGeneratorComponent component, ref AnchorStateChangedEvent args)
        {
            if (!args.Anchored)
            {
                component.Connection1?.Item2.Dispose();
                component.Connection2?.Item2.Dispose();
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
            if (_tags.HasTag(args.OtherFixture.Body.Owner, "EmitterBolt"))
            {
                ReceivePower(6, component);
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

        public void ReceivePower(int power, ContainmentFieldGeneratorComponent component)
        {
            var totalPower = power + component.PowerBuffer;
            var powerPerConnection = totalPower / 2;
            var newBuffer = totalPower % 2;
            TryPowerConnection(ref component.Connection1, ref newBuffer, powerPerConnection, component);
            TryPowerConnection(ref component.Connection2, ref newBuffer, powerPerConnection, component);

            component.PowerBuffer = newBuffer;
        }

        public void UpdateConnectionLights(ContainmentFieldGeneratorComponent component)
        {
            if (EntityManager.TryGetComponent<PointLightComponent>(component.Owner, out var pointLightComponent))
            {
                bool hasAnyConnection = (component.Connection1 != null) || (component.Connection2 != null);
                pointLightComponent.Enabled = hasAnyConnection;
            }
        }

        public void RemoveConnection(ContainmentFieldConnection? connection, ContainmentFieldGeneratorComponent component)
        {
            if (component.Connection1?.Item2 == connection)
            {
                component.Connection1 = null;
                UpdateConnectionLights(component);
            }
            else if (component.Connection2?.Item2 == connection)
            {
                component.Connection2 = null;
                UpdateConnectionLights(component);
            }
            else if (connection != null)
            {
                Logger.Error("RemoveConnection called on Containmentfieldgenerator with a connection that can't be found in its connections.");
            }
        }

        private bool TryGenerateFieldConnection([NotNullWhen(true)] ref Tuple<Direction, ContainmentFieldConnection>? propertyFieldTuple, ContainmentFieldGeneratorComponent component)
        {
            if (propertyFieldTuple != null) return false;
            if (EntityManager.TryGetComponent<TransformComponent>(component.Owner, out var xform) && !xform.Anchored) return false;

            foreach (var direction in new[] { Direction.North, Direction.East, Direction.South, Direction.West })
            {
                if (component.Connection1?.Item1 == direction || component.Connection2?.Item1 == direction) continue;

                var dirVec = EntityManager.GetComponent<TransformComponent>(component.Owner).WorldRotation.RotateVec(direction.ToVec());
                var ray = new CollisionRay(EntityManager.GetComponent<TransformComponent>(component.Owner).WorldPosition, dirVec, (int) CollisionGroup.MobMask);
                var rawRayCastResults = EntitySystem.Get<SharedPhysicsSystem>().IntersectRay(EntityManager.GetComponent<TransformComponent>(component.Owner).MapID, ray, 4.5f, component.Owner, false);

                var rayCastResults = rawRayCastResults as RayCastResults[] ?? rawRayCastResults.ToArray();
                if (!rayCastResults.Any()) continue;

                RayCastResults? closestResult = null;
                var smallestDist = 4.5f;
                foreach (var res in rayCastResults)
                {
                    if (res.Distance > smallestDist) continue;

                    smallestDist = res.Distance;
                    closestResult = res;
                }
                if (closestResult == null) continue;
                var ent = closestResult.Value.HitEntity;
                if (!EntityManager.TryGetComponent<ContainmentFieldGeneratorComponent?>(ent, out var fieldGeneratorComponent) ||
                    fieldGeneratorComponent.Owner == component.Owner ||
                    !HasFreeConnections(fieldGeneratorComponent) ||
                    IsConnectedWith(component, fieldGeneratorComponent) ||
                    !EntityManager.TryGetComponent<PhysicsComponent?>(ent, out var collidableComponent) ||
                    collidableComponent.BodyType != BodyType.Static)
                {
                    continue;
                }

                var connection = new ContainmentFieldConnection(component, fieldGeneratorComponent);
                propertyFieldTuple = new Tuple<Direction, ContainmentFieldConnection>(direction, connection);
                if (fieldGeneratorComponent.Connection1 == null)
                {
                    fieldGeneratorComponent.Connection1 = new Tuple<Direction, ContainmentFieldConnection>(direction.GetOpposite(), connection);
                }
                else if (fieldGeneratorComponent.Connection2 == null)
                {
                    fieldGeneratorComponent.Connection2 = new Tuple<Direction, ContainmentFieldConnection>(direction.GetOpposite(), connection);
                }
                else
                {
                    Logger.Error("When trying to connect two Containmentfieldgenerators, the second one already had two connection but the check didn't catch it");
                }
                UpdateConnectionLights(component);
                return true;
            }

            return false;
        }

        private bool IsConnectedWith(ContainmentFieldGeneratorComponent comp, ContainmentFieldGeneratorComponent otherComp)
        {
            return otherComp == comp || comp.Connection1?.Item2.Generator1 == otherComp || comp.Connection1?.Item2.Generator2 == otherComp ||
                   comp.Connection2?.Item2.Generator1 == otherComp || comp.Connection2?.Item2.Generator2 == otherComp;
        }

        public void TryPowerConnection(ref Tuple<Direction, ContainmentFieldConnection>? connectionProperty, ref int powerBuffer, int powerPerConnection, ContainmentFieldGeneratorComponent component)
        {
            if (connectionProperty != null)
            {
                connectionProperty.Item2.SharedEnergyPool += powerPerConnection;
            }
            else
            {
                if (TryGenerateFieldConnection(ref connectionProperty, component))
                {
                    connectionProperty.Item2.SharedEnergyPool += powerPerConnection;
                }
                else
                {
                    powerBuffer += powerPerConnection;
                }
            }
        }

        public bool CanRepel(SharedSingularityComponent toRepel, ContainmentFieldGeneratorComponent component) => component.Connection1?.Item2?.CanRepel(toRepel) == true ||
                                                                     component.Connection2?.Item2?.CanRepel(toRepel) == true;

        public bool HasFreeConnections(ContainmentFieldGeneratorComponent component)
        {
            return component.Connection1 == null || component.Connection2 == null;
        }


    }
}
