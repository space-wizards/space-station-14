using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Robust.Shared.GameObjects.Systems;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Server.Interfaces.Timing;

namespace Content.Server.Interfaces.GameObjects.Components.Interaction
{
    [UsedImplicitly]
    internal sealed class PowerApcSystem : EntitySystem
    {
        [Dependency] private readonly IPauseManager _pauseManager = default!;

        public override void Update(float frameTime)
        {
            var uniqueApcNets = new HashSet<IApcNet>(); //could be improved by maintaining set instead of getting collection every frame 
            foreach (var apc in ComponentManager.EntityQuery<ApcComponent>())
            {
                if (_pauseManager.IsEntityPaused(apc.Owner))
                {
                    continue;
                }

                uniqueApcNets.Add(apc.Net);
                apc.Update();
            }
            
            foreach (var apcNet in uniqueApcNets)
            {
                apcNet.Update(frameTime);
            }
        }
    }
}
