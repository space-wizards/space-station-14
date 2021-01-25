using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using System.Linq;
using Content.Server.GameObjects.Components.Radiation;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var component in ComponentManager.EntityQuery<RadiationPulseComponent>(false))
            {
                component.Update(frameTime);

                var radiationSourceEntity = component.Owner;

                if (radiationSourceEntity.Deleted || component.OnCooldown(out var ratio))
                {
                    continue;
                }

                foreach (var affectedEntity in EntityManager.GetEntitiesInRange(
                    radiationSourceEntity.Transform.Coordinates, component.Range, true))
                {
                    if (affectedEntity.Deleted)
                    {
                        return;
                    }

                    var canBeRadiated = affectedEntity.GetAllComponents<IRadiationAct>()
                        .Where(e => !e.Deleted);
                    if (canBeRadiated.Any() && component.CanRadiate(affectedEntity))
                    {
                        foreach (var radComponent in canBeRadiated)
                        {
                            radComponent.RadiationAct(component, ratio);
                        }
                    }
                }
            }
        }

	}
}
