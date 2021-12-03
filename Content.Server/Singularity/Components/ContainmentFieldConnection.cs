using System;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server.Singularity.Components
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
                _sharedEnergyPool = Math.Clamp(value, 0, 25);
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
            var pos1 = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(generator1.Owner.Uid).Coordinates;
            var pos2 = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(generator2.Owner.Uid).Coordinates;
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
                if (!IoCManager.Resolve<IEntityManager>().TryGetComponent<ContainmentFieldComponent?>(newEnt.Uid, out var containmentFieldComponent))
                {
                    Logger.Error("While creating Fields in ContainmentFieldConnection, a ContainmentField without a ContainmentFieldComponent was created. Deleting newly spawned ContainmentField...");
                    IoCManager.Resolve<IEntityManager>().DeleteEntity(newEnt.Uid);
                    continue;
                }

                containmentFieldComponent.Parent = this;
                IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).WorldRotation = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(generator1.Owner.Uid).WorldRotation + dirVec.ToWorldAngle();

                _fields.Add(newEnt);
                currentOffset += dirVec;
            }


            Timer.SpawnRepeating(1000, () => { SharedEnergyPool--;}, _powerDecreaseCancellationTokenSource.Token);
        }

        public bool CanRepell(IEntity toRepell)
        {
            var powerNeeded = 1;
            if (IoCManager.Resolve<IEntityManager>().TryGetComponent<ServerSingularityComponent?>(toRepell.Uid, out var singularityComponent))
            {
                powerNeeded += 2*singularityComponent.Level;
            }

            return _sharedEnergyPool > powerNeeded;
        }

        public void Dispose()
        {
            _powerDecreaseCancellationTokenSource.Cancel();
            foreach (var field in _fields)
            {
                IoCManager.Resolve<IEntityManager>().DeleteEntity(field.Uid);
            }
            _fields.Clear();

            Generator1.RemoveConnection(this);
            Generator2.RemoveConnection(this);
        }
    }
}
