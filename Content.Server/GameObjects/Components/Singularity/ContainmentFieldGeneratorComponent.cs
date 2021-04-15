#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.GameObjects.Components.Tag;
using Content.Shared.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;
using Robust.Shared.ViewVariables;
using Robust.Server.GameObjects;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Dynamics;

namespace Content.Server.GameObjects.Components.Singularity
{
    [RegisterComponent]
    public class ContainmentFieldGeneratorComponent : Component, IStartCollide
    {
        public override string Name => "ContainmentFieldGenerator";

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

        [ComponentDependency] private readonly PhysicsComponent? _collidableComponent = default;
        [ComponentDependency] private readonly PointLightComponent? _pointLightComponent = default;

        private Tuple<Direction, ContainmentFieldConnection>? _connection1;
        private Tuple<Direction, ContainmentFieldConnection>? _connection2;

        public bool CanRepell(IEntity toRepell) => _connection1?.Item2?.CanRepell(toRepell) == true ||
                                                   _connection2?.Item2?.CanRepell(toRepell) == true;

        public override void HandleMessage(ComponentMessage message, IComponent? component)
        {
            base.HandleMessage(message, component);
            switch (message)
            {
                case AnchoredChangedMessage:
                    OnAnchoredChanged();
                    break;
            }
        }

        private void OnAnchoredChanged()
        {
            if(_collidableComponent?.BodyType != BodyType.Static)
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
            if(_collidableComponent?.BodyType != BodyType.Static) return false;

            foreach (var direction in new[] {Direction.North, Direction.East, Direction.South, Direction.West})
            {
                if (_connection1?.Item1 == direction || _connection2?.Item1 == direction) continue;

                var dirVec = direction.ToVec();
                var ray = new CollisionRay(Owner.Transform.WorldPosition, dirVec, (int) CollisionGroup.MobMask);
                var rawRayCastResults = EntitySystem.Get<SharedBroadPhaseSystem>().IntersectRay(Owner.Transform.MapID, ray, 4.5f, Owner, false);

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
                if (!ent.TryGetComponent<ContainmentFieldGeneratorComponent>(out var fieldGeneratorComponent) ||
                    fieldGeneratorComponent.Owner == Owner ||
                    !fieldGeneratorComponent.HasFreeConnections() ||
                    IsConnectedWith(fieldGeneratorComponent) ||
                    !ent.TryGetComponent<PhysicsComponent>(out var collidableComponent) ||
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

        void IStartCollide.CollideWith(Fixture ourFixture, Fixture otherFixture, in Manifold manifold)
        {
			if(otherFixture.Body.Owner.HasTag("EmitterBolt"))            {
                ReceivePower(4);
            }
        }

        public void UpdateConnectionLights()
        {
            if (_pointLightComponent != null)
            {
                bool hasAnyConnection = (_connection1 != null) || (_connection2 != null);
                _pointLightComponent.Enabled = hasAnyConnection;
            }
        }

        public override void OnRemove()
        {
            _connection1?.Item2.Dispose();
            _connection2?.Item2.Dispose();
            base.OnRemove();
        }
    }
}
