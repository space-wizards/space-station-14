#nullable enable
using System.Collections.Generic;
using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.ApcNetComponents;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    internal sealed class PowerApcSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            var uniqueApcNets = new HashSet<IApcNet>(); //could be improved by maintaining set instead of getting collection every frame
            foreach (var apc in ComponentManager.EntityQuery<ApcComponent>(false))
            {
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
