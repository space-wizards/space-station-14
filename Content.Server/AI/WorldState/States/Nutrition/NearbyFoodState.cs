using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Nutrition.Components;
using Content.Server.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    [UsedImplicitly]
    public sealed class NearbyFoodState : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyFood";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(Owner, out NPCComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(entMan.GetComponent<TransformComponent>(Owner).Coordinates, typeof(FoodComponent), controller.VisionRadius))
            {
                if (entity.TryGetContainer(out var container))
                {
                    if (!entMan.HasComponent<EntityStorageComponent>(container.Owner))
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
