using System.Collections.Generic;
using Content.Server.AI.Utils;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Power.Chargers;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Combat.Nearby
{
    [UsedImplicitly]
    public sealed class NearbyLaserChargersState : StateData<List<IEntity>>
    {
        public override string Name => "NearbyLaserChargers";

        public override List<IEntity> GetValue()
        {
            var nearby = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return nearby;
            }

            foreach (var result in Visibility
                .GetNearestEntities(Owner.Transform.GridPosition, typeof(WeaponCapacitorChargerComponent), controller.VisionRadius))
            {
                nearby.Add(result);
            }

            return nearby;
        }
    }
}
