using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Server.Interfaces.Timing;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    public sealed class ApcSystem : EntitySystem
    {
#pragma warning disable 649
        [Dependency] private readonly IPauseManager _pauseManager;
#pragma warning restore 649

        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(ApcComponent));
        }

        public override void Update(float frameTime)
        {
            var uniqueApcNets = new HashSet<IApcNet>(); //could be improved by maintaining set instead of getting collection every frame 
            foreach (var entity in RelevantEntities)
            {
                if (_pauseManager.IsEntityPaused(entity))
                {
                    continue;
                }
                var apc = entity.GetComponent<ApcComponent>();
                uniqueApcNets.Add(apc.Net);
                entity.GetComponent<ApcComponent>().Update();
            }
            foreach (var apcNet in uniqueApcNets)
            {
                apcNet.Update(frameTime);
            }
        }
    }
}
