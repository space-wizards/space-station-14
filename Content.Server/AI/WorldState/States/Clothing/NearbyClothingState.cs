using System.Collections.Generic;
using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Clothing.Components;
using Content.Server.Storage.Components;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.AI.WorldState.States.Clothing
{
    [UsedImplicitly]
    public sealed class NearbyClothingState : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyClothing";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();

            if (!IoCManager.Resolve<IEntityManager>().TryGetComponent(Owner, out AiControllerComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates, typeof(ClothingComponent), controller.VisionRadius))
            {
                if (entity.TryGetContainer(out var container))
                {
                    if (!IoCManager.Resolve<IEntityManager>().HasComponent<EntityStorageComponent>(container.Owner))
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
