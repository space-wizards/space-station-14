using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Despite the name, it's only really used for the welder logic in tools. Go figure.
    /// </summary>
    public class ToolSystem : EntitySystem
    {
        private readonly HashSet<ToolComponent> _activeWelders = new HashSet<ToolComponent>();

        public bool Subscribe(ToolComponent welder)
        {
            return _activeWelders.Add(welder);
        }

        public bool Unsubscribe(ToolComponent welder)
        {
            return _activeWelders.Remove(welder);
        }

        public override void Update(float frameTime)
        {
            foreach (var tool in _activeWelders.ToArray())
            {
                tool.OnUpdate(frameTime);
            }
        }
    }
}
