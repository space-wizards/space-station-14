using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Shared.Body.Components;
using JetBrains.Annotations;

namespace Content.Server.AI.WorldState.States.Mobs
{
    [UsedImplicitly]
    public sealed class NearbyBodiesState : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyBodies";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(Owner, out NPCComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility.GetEntitiesInRange(entMan.GetComponent<TransformComponent>(Owner).Coordinates, typeof(SharedBodyComponent), controller.VisionRadius))
            {
                if (entity == Owner) continue;
                result.Add(entity);
            }

            return result;
        }
    }
}
