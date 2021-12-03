using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Nutrition.Components;
using Content.Server.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public sealed class NearbyFoodState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyFood";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner.Uid, out AiControllerComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner.Uid).Coordinates, typeof(FoodComponent), controller.VisionRadius))
            {
                if (entity.TryGetContainer(out var container))
                {
                    if (!IoCManager.Resolve<IEntityManager>().HasComponent<EntityStorageComponent>(container.Owner.Uid))
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
