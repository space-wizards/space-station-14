using Content.Server.GameObjects.Components.NewPower.ApcNetComponents;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using System.Collections.Generic;

namespace Content.Server.GameObjects.EntitySystems
{
    public sealed class ApcSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ApcComponent));
        }

        public override void Update(float frameTime)
        {
            var uniqueApcNets = new HashSet<IApcNet>();
            foreach (var entity in RelevantEntities)
            {
                uniqueApcNets.Add(entity.GetComponent<ApcComponent>().Net);
            }
            foreach (var apcNet in uniqueApcNets)
            {
                apcNet.Update(frameTime);
            }
        }
    }
}
