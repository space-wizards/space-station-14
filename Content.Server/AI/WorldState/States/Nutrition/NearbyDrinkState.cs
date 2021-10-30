using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Nutrition.Components;
using Content.Server.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public sealed class NearbyDrinkState: CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyDrink";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(Owner.Transform.Coordinates, typeof(DrinkComponent), controller.VisionRadius))
            {
                if (entity.TryGetContainer(out var container))
                {
                    if (!container.Owner.HasComponent<EntityStorageComponent>())
                    {
                        continue;
                    }
                }
                result.Add(entity);
            }

            return result;
        }
    }
}
