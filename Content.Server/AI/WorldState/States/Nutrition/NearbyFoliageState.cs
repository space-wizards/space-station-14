using Content.Server.AI.Components;
using Content.Server.AI.Utils;
using Content.Server.Nyanotrasen.Nutrition.Components;

namespace Content.Server.AI.WorldState.States.Nutrition
{
    public sealed class NearbyFoliageState : CachedStateData<List<EntityUid>>
    {
        public override string Name => "NearbyFoliage";

        protected override List<EntityUid> GetTrueValue()
        {
            var result = new List<EntityUid>();
            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent(Owner, out NPCComponent? controller))
            {
                return result;
            }

            foreach (var entity in Visibility
                .GetNearestEntities(entMan.GetComponent<TransformComponent>(Owner).Coordinates, typeof(FoliageComponent), controller.VisionRadius))
            {
                if (entMan.HasComponent<FoliageComponent>(entity))
                {
                    result.Add(entity);
                }
            }

            return result;
        }
    }
}
