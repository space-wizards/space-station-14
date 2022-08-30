using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Clothing.Components;
using Content.Server.Storage.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;

namespace Content.Server.AI.WorldState.States.Clothing
{
    [UsedImplicitly]
    public sealed class NearbyClothingState : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyClothing";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent(Owner, out NPCComponent? controller))
            {
                return result;
            }
            var containerSystem = entMan.EntitySysManager.GetEntitySystem<ContainerSystem>();
            foreach (var entity in Visibility.GetNearestEntities(entMan.GetComponent<TransformComponent>(Owner).Coordinates, typeof(ClothingComponent), controller.VisionRadius))
            {
                if (containerSystem.TryGetContainingContainer(entity, out var container))
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
