using System.Collections.Generic;
using Content.Server.AI.Utils;
using Content.Server.GameObjects.Components.Items.Clothing;
using Content.Server.GameObjects.Components.Items.Storage;
using Content.Server.GameObjects.Components.Movement;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Server.AI.WorldState.States.Clothing
{
    [UsedImplicitly]
    public sealed class NearbyClothingState : CachedStateData<List<IEntity>>
    {
        public override string Name => "NearbyClothing";

        protected override List<IEntity> GetTrueValue()
        {
            var result = new List<IEntity>();

            if (!Owner.TryGetComponent(out AiControllerComponent controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(Owner.Transform.Coordinates, typeof(ClothingComponent), controller.VisionRadius))
            {
                if (ContainerHelpers.TryGetContainer(entity, out var container))
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
