using System;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.GameObjects.Components.Singularity
{
    public class ContainmentFieldConnection : IDisposable
    {
        public readonly ContainmentFieldGeneratorComponent Generator1;
        public readonly ContainmentFieldGeneratorComponent Generator2;
        private readonly List<IEntity> _fields = new();
        private int _sharedEnergyPool;
        private readonly CancellationTokenSource _powerDecreaseCancellationTokenSource = new();
        public int SharedEnergyPool
        {
            get => _sharedEnergyPool;
            set
            {
                _sharedEnergyPool = Math.Clamp(value, 0, 10);
                if (_sharedEnergyPool == 0)
                {
                    Dispose();
                }
            }
        }

        public ContainmentFieldConnection(ContainmentFieldGeneratorComponent generator1, ContainmentFieldGeneratorComponent generator2)
        {
            Generator1 = generator1;
            Generator2 = generator2;

            //generateFields
            var pos1 = generator1.Owner.Transform.Coordinates;
            var pos2 = generator2.Owner.Transform.Coordinates;
            if (pos1 == pos2)
            {
                Dispose();
                return;
            }

            var entityManager = IoCManager.Resolve<IEntityManager>();

            var delta = (pos2 - pos1).Position;
            var dirVec = delta.Normalized;
            var stopDist = delta.Length;
            var currentOffset = dirVec;
            while (currentOffset.Length < stopDist)
            {
                var currentCoords = pos1.Offset(currentOffset);
                var newEnt = entityManager.SpawnEntity("ContainmentField", currentCoords);
                if (!newEnt.TryGetComponent<ContainmentFieldComponent>(out var containmentFieldComponent))
                {
                    Logger.Error("While creating Fields in ContainmentFieldConnection, a ContainmentField without a ContainmentFieldComponent was created. Deleting newly spawned ContainmentField...");
                    newEnt.Delete();
                    continue;
                }

                containmentFieldComponent.Parent = this;
                newEnt.Transform.WorldRotation = dirVec.ToWorldAngle();

                _fields.Add(newEnt);
                currentOffset += dirVec;
            }


            Timer.SpawnRepeating(1000, () => { SharedEnergyPool--;}, _powerDecreaseCancellationTokenSource.Token);
        }

        public bool CanRepell(IEntity toRepell)
        {
            var powerNeeded = 1;
            if (toRepell.TryGetComponent<ServerSingularityComponent>(out var singularityComponent))
            {
                powerNeeded += 2*singularityComponent.Level;
            }

            return _sharedEnergyPool > powerNeeded;
        }

        /// <summary>
        /// Tries to repell a Entity. This deletes the connection if the repelling fails!
        /// </summary>
        /// <param name="repellFrom">Entity to repell from. Should be a field, otherwise return will be false.</param>
        /// <param name="toRepell">Entity to repell.</param>
        public void TryRepell(IEntity repellFrom, IEntity toRepell)
        {
            // TODO: Fix this also it's fucking repel
            if (!_fields.Contains(repellFrom) || !toRepell.TryGetComponent<IPhysBody>(out var collidableComponent)) return;

            return;
            var speed = 5;
            //var containmentFieldRepellController = collidableComponent.EnsureController<ContainmentFieldRepellController>();

            if (!CanRepell(toRepell))
            {
                Dispose();
                return;
            }

            if (Math.Abs(repellFrom.Transform.WorldRotation.Degrees + 90f) < 0.1f ||
                Math.Abs(repellFrom.Transform.WorldRotation.Degrees - 90f) < 0.1f)
            {
                if (repellFrom.Transform.WorldPosition.X.CompareTo(toRepell.Transform.WorldPosition.X) > 0)
                {
                    //containmentFieldRepellController.Repell(Direction.West, speed);
                }
                else
                {
                    //containmentFieldRepellController.Repell(Direction.East, speed);
                }
            }
            else
            {
                if (repellFrom.Transform.WorldPosition.Y.CompareTo(toRepell.Transform.WorldPosition.Y) > 0)
                {
                    //containmentFieldRepellController.Repell(Direction.South, speed);
                }
                else
                {
                    //containmentFieldRepellController.Repell(Direction.North, speed);
                }
            }

            return;
        }

        public void Dispose()
        {
            _powerDecreaseCancellationTokenSource.Cancel();
            foreach (var field in _fields)
            {
                field.Delete();
            }
            _fields.Clear();

            Generator1.RemoveConnection(this);
            Generator2.RemoveConnection(this);
        }
    }
}
