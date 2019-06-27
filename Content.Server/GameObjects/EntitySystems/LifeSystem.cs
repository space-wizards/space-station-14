using System;
using System.Text;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using Content.Server.GameObjects.Components.Mobs;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Server.Interfaces.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Input;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Players;
using Robust.Shared.Utility;


namespace Content.Server.GameObjects.EntitySystems
{
    public class LifeSystem : EntitySystem
    {
        IGameTiming _gameTick;
        int lifeTickRate = 2; // 2 calls every second
        int lifeTicks;
        public override void Initialize()
        {
            _gameTick = IoCManager.Resolve<IGameTiming>();
            EntityQuery = new TypeEntityQuery(typeof(MobComponent));
            lifeTicks = 60 / lifeTickRate; //How to get current tickrate √∫
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                if (_gameTick.CurTick.Value % lifeTicks == 0)
                {
                    var comp = entity.GetComponent<MobComponent>();
                    comp.OnUpdate();
                }
            }
        }
    }
}
