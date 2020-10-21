using Content.Server.GameObjects.Components.Botany;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class PlantSystem : EntitySystem
    {
        [Dependency] private readonly IComponentManager _componentManager = default!;

        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var plantHolder in _componentManager.EntityQuery<PlantHolderComponent>())
            {
                plantHolder.Update(frameTime);
            }
        }
    }
}
