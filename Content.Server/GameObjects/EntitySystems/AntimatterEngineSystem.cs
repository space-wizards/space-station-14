using Content.Server.GameObjects.Components.NodeContainer.NodeGroups;
using Content.Server.GameObjects.Components.Power.AME;
using JetBrains.Annotations;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.Configuration;
using Robust.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Server.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class AntimatterEngineSystem : EntitySystem
    {
        [Dependency] private readonly IConfigurationManager _config = default!;
        private int _injectionTickCounter = 0;

        public override void Initialize()
        {
            base.Initialize();
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            _injectionTickCounter++;
            if (_injectionTickCounter >= 5 * (_config.GetCVar<int>("net.tickrate")))
            {
                foreach (var comp in ComponentManager.EntityQuery<AMEControllerComponent>())
                {
                    comp.OnUpdate(frameTime);
                }
                _injectionTickCounter = 0;
            }

        }
    }
}
