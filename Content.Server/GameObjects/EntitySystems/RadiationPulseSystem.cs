using Content.Shared.Interfaces.GameObjects.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Radiation;
using Robust.Shared.Interfaces.Random;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        private readonly Dictionary<IComponent, float> _frequencyTrigger = new();

        public override void Initialize()
        {
            base.Initialize();
            IoCManager.InjectDependencies(this);
        }

        /// <summary>
        /// Checks if the radiation pulse can radiate again, the frequency of the hits* is defined by
        /// <see cref="RadiationPulseComponent.Cooldown"/>
        /// </summary>
        /// <returns> True/False if the pulse can radiate..
        /// </returns>
        private bool IsPulseInCooldown(RadiationPulseComponent component, float frameTime)
        {
            if (_frequencyTrigger.ContainsKey(component))
            {
                if (_frequencyTrigger[component] <= 0.0f)
                {
                    _frequencyTrigger[component] = component.Cooldown;
                    return false;
                }
                _frequencyTrigger[component] -= frameTime;
                return true;
            }
            else
            {
                _frequencyTrigger.Add(component, component.Cooldown);
                return false;
            }
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var component in ComponentManager.EntityQuery<RadiationPulseComponent>())
            {
                component.Update(frameTime);

                var radiationAnomalyEntity = component.Owner;

                if (radiationAnomalyEntity.Deleted)
                {
                    _frequencyTrigger.Remove(component);
                    continue;
                }

                if (IsPulseInCooldown(component, frameTime))
                {
                    continue;
                }

                foreach (var affectedEntity in EntityManager.GetEntitiesInRange(
                    radiationAnomalyEntity.Transform.Coordinates, component.Range, true))
                {
                    if (affectedEntity.Deleted)
                    {
                        return;
                    }

                    var canBeRadiated = affectedEntity.GetAllComponents<IRadiationAct>().Where(e => !e.Deleted);
                    if (canBeRadiated.Any() && component.CanRadiate(affectedEntity))
                    {
                        foreach (var radComponent in canBeRadiated)
                        {
                            radComponent.RadiationAct(component);
                        }
                    }
                }
            }
        }

	}
}
