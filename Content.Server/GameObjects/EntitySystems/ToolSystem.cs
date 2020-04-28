using System.Collections.Generic;
using Content.Server.GameObjects.Components.Interactable;
using Content.Server.GameObjects.Components.Interactable.Tools;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Despite the name, it's only really used for the welder logic in tools. Go figure.
    /// </summary>
    public class ToolSystem : EntitySystem
    {
        private readonly HashSet<ToolComponent> _activeWelders = new HashSet<ToolComponent>();

        public override void Update(float frameTime)
        {
            foreach (var tool in _activeWelders) ;
        }
    }
}
