using Content.Server.GameObjects.Components.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    public class GasAnalyzerSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var analyzer in ComponentManager.EntityQuery<GasAnalyzerComponent>())
            {
                analyzer.Update(frameTime);
            }
        }
    }
}
