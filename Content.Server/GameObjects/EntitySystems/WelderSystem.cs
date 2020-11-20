using System.Collections.Generic;
using System.Linq;
using Content.Server.GameObjects.Components.Interactable;
using Robust.Shared.GameObjects.Systems;

namespace Content.Server.GameObjects.EntitySystems
{
    /// <summary>
    ///     Despite the name, it's only really used for the welder logic in tools. Go figure.
    /// </summary>
    public class WelderSystem : EntitySystem
    {
        private readonly HashSet<WelderComponent> _activeWelders = new();

        public bool Subscribe(WelderComponent welder)
        {
            return _activeWelders.Add(welder);
        }

        public bool Unsubscribe(WelderComponent welder)
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
