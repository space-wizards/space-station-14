using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.Physics;
using Content.Shared.Singularity.Components;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.ViewVariables;

namespace Content.Server.Singularity.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedContainmentFieldGeneratorComponent))]
    public sealed class ContainmentFieldGeneratorComponent : SharedContainmentFieldGeneratorComponent
    {
        [Dependency] private readonly IEntityManager _entMan = default!;

        private int _powerBuffer;

        [ViewVariables]
        public int PowerBuffer
        {
            get => _powerBuffer;
            set => _powerBuffer = Math.Clamp(value, 0, 6);
        }

        public void ReceivePower(int power)
        {
            var totalPower = power + PowerBuffer;
            var powerPerConnection = totalPower  / 2;
            var newBuffer = totalPower % 2;
            TryPowerConnection(ref _connection1, ref newBuffer, powerPerConnection);
            TryPowerConnection(ref _connection2, ref newBuffer, powerPerConnection);

            PowerBuffer = newBuffer;
        }

        private void TryPowerConnection(ref Tuple<Direction, ContainmentFieldConnection>? connectionProperty, ref int powerBuffer, int powerPerConnection)
        {
            if (connectionProperty != null)
            {
                connectionProperty.Item2.SharedEnergyPool += powerPerConnection;
            }
            else
            {
                if (TryGenerateFieldConnection(ref connectionProperty))
                {
                    connectionProperty.Item2.SharedEnergyPool += powerPerConnection;
                }
                else
                {
                    powerBuffer += powerPerConnection;
                }
            }
        }

        private Tuple<Direction, ContainmentFieldConnection>? _connection1;
        private Tuple<Direction, ContainmentFieldConnection>? _connection2;

        public bool CanRepell(EntityUid toRepell) => _connection1?.Item2?.CanRepell(toRepell) == true ||
                                                   _connection2?.Item2?.CanRepell(toRepell) == true;

        public void OnAnchoredChanged()
        {
            if(_entMan.TryGetComponent<PhysicsComponent>(Owner, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static)
            {
                _connection1?.Item2.Dispose();
                _connection2?.Item2.Dispose();
            }
        }

        private bool IsConnectedWith(ContainmentFieldGeneratorComponent comp)
        {

            return comp == this || _connection1?.Item2.Generator1 == comp || _connection1?.Item2.Generator2 == comp ||
                   _connection2?.Item2.Generator1 == comp || _connection2?.Item2.Generator2 == comp;
        }

        public bool HasFreeConnections()
        {
            return _connection1 == null || _connection2 == null;
        }

        private bool TryGenerateFieldConnection([NotNullWhen(true)] ref Tuple<Direction, ContainmentFieldConnection>? propertyFieldTuple)
        {
            if (propertyFieldTuple != null) return false;
            if(_entMan.TryGetComponent<PhysicsComponent>(Owner, out var physicsComponent) && physicsComponent.BodyType != BodyType.Static) return false;

            foreach (var direction in new[] {Direction.North, Direction.East, Direction.South, Direction.West})
            {
                if (_connection1?.Item1 == direction || _connection2?.Item1 == direction) continue;

                var dirVec = _entMan.GetComponent<TransformComponent>(Owner).WorldRotation.RotateVec(direction.ToVec());
                var ray = new CollisionRay(_entMan.GetComponent<TransformComponent>(Owner).WorldPosition, dirVec, (int) CollisionGroup.MobMask);
                var rawRayCastResults = EntitySystem.Get<SharedPhysicsSystem>().IntersectRay(_entMan.GetComponent<TransformComponent>(Owner).MapID, ray, 4.5f, Owner, false);

                var rayCastResults = rawRayCastResults as RayCastResults[] ?? rawRayCastResults.ToArray();
                if(!rayCastResults.Any()) continue;

                RayCastResults? closestResult = null;
                var smallestDist = 4.5f;
                foreach (var res in rayCastResults)
                {
                    if (res.Distance > smallestDist) continue;

                    smallestDist = res.Distance;
                    closestResult = res;
                }
                if(closestResult == null) continue;
                var ent = closestResult.Value.HitEntity;
                if (!_entMan.TryGetComponent<ContainmentFieldGeneratorComponent?>(ent, out var fieldGeneratorComponent) ||
                    fieldGeneratorComponent.Owner == Owner ||
                    !fieldGeneratorComponent.HasFreeConnections() ||
                    IsConnectedWith(fieldGeneratorComponent) ||
                    !_entMan.TryGetComponent<PhysicsComponent?>(ent, out var collidableComponent) ||
                    collidableComponent.BodyType != BodyType.Static)
                {
                    continue;
                }

                var connection = new ContainmentFieldConnection(this, fieldGeneratorComponent);
                propertyFieldTuple = new Tuple<Direction, ContainmentFieldConnection>(direction, connection);
                if (fieldGeneratorComponent._connection1 == null)
                {
                    fieldGeneratorComponent._connection1 = new Tuple<Direction, ContainmentFieldConnection>(direction.GetOpposite(), connection);
                }
                else if (fieldGeneratorComponent._connection2 == null)
                {
                    fieldGeneratorComponent._connection2 = new Tuple<Direction, ContainmentFieldConnection>(direction.GetOpposite(), connection);
                }
                else
                {
                    Logger.Error("When trying to connect two Containmentfieldgenerators, the second one already had two connection but the check didn't catch it");
                }
                UpdateConnectionLights();
                return true;
            }

            return false;
        }

        public void RemoveConnection(ContainmentFieldConnection? connection)
        {
            if (_connection1?.Item2 == connection)
            {
                _connection1 = null;
                UpdateConnectionLights();
            }
            else if (_connection2?.Item2 == connection)
            {
                _connection2 = null;
                UpdateConnectionLights();
            }
            else if(connection != null)
            {
                Logger.Error("RemoveConnection called on Containmentfieldgenerator with a connection that can't be found in its connections.");
            }
        }

        public void UpdateConnectionLights()
        {
            if (_entMan.TryGetComponent<PointLightComponent>(Owner, out var pointLightComponent))
            {
                bool hasAnyConnection = (_connection1 != null) || (_connection2 != null);
                pointLightComponent.Enabled = hasAnyConnection;
            }
        }

        protected override void OnRemove()
        {
            _connection1?.Item2.Dispose();
            _connection2?.Item2.Dispose();
            base.OnRemove();
        }
    }
}
