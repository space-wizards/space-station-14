using System;
using System.Text;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.Input;
using Content.Server.GameObjects.Components.Mobs.Body;
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
    public interface IOnLife
    {
        void OnUpdate(float frameTime);
    }

    public class LifeSystem : EntitySystem
    {
        public override void Initialize()
        {
            EntityQuery = new TypeEntityQuery(typeof(IOnLife));
        }

        public override void Update(float frameTime)
        {
            foreach (var entity in RelevantEntities)
            {
                var comp = entity.GetComponent<IOnLife>();
                comp.OnUpdate(frameTime);
            }
        }
    }
}
