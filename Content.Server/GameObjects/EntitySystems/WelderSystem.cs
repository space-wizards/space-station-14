using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Despite the name, it's only really used for the welder logic in tools. Go figure.
    /// </summary>
    public class WelderSystem : EntitySystem
    {
        public override void Update(float frameTime)
        {
            foreach (var welder in EntityManager.ComponentManager.EntityQuery<WelderComponent>())
            {
                if(welder.WelderLit && !welder.Owner.Deleted)
                    welder.OnUpdate(frameTime);
            }
        }
    }
}
