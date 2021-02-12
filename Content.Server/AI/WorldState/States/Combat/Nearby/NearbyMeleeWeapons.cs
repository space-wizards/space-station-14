using System.Collections.Generic;
using Content.Server.AI.Utils;
using Content.Server.GameObjects.Components.Movement;
using Content.Server.GameObjects.Components.Weapon.Melee;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States.Combat.Nearby
{
    [UsedImplicitly]
    public sealed class NearbyMeleeWeapons : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyMeleeWeapons";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(Owner.Transform.Coordinates, typeof(MeleeWeaponComponent), controller.VisionRadius))
            {
                result.Add(entity);
            }

            return result;
        }
    }
}
