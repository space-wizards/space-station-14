using System.Collections.Generic;
using Content.Server.AI.Utils;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Weapon.Ranged;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Combat.Nearby
{
    [UsedImplicitly]
    public sealed class NearbyRangedWeapons : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyRangedWeapons";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(Owner.Transform.GridPosition, typeof(RangedWeaponComponent), controller.VisionRadius))
            {
                result.Add(entity);
            }

            return result;
        }
    }
}
