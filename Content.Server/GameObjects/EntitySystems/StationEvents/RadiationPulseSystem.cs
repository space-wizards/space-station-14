using System.Collections.Generic;
using Content.Server.GameObjects.Components.Damage;
using Content.Server.GameObjects.Components.Mobs;
using Content.Server.GameObjects.Components.StationEvents;
using Content.Server.Interfaces.GameObjects.Components;
using Content.Shared.Damage;
using Content.Shared.GameObjects;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server.GameObjects.EntitySystems.StationEvents
{
    [UsedImplicitly]
    public sealed class RadiationPulseSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            base.Update(frameTime);

            foreach (var comp in ComponentManager.EntityQuery<RadiationPulseComponent>())
            {
                comp.Update(frameTime);
                var ent = comp.Owner;

                foreach (var entity in EntityManager.GetEntitiesInRange(ent.Transform.GridPosition, comp.Range, true))
                {
                    foreach (var radiation in entity.GetAllComponents<IRadiationAct>())
                    {
                        radiation.RadiationAct(frameTime, comp);
                    }
                }
            }
        }
    }
}
