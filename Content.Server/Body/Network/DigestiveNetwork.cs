using Content.Server.GameObjects.Components.Body.Digestive;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Network
{
    /// <summary>
    ///     Represents the system that processes food, liquids, and the reagents inside them.
    /// </summary>
    [UsedImplicitly]
    public class DigestiveNetwork : BodyNetwork
    {
        public override string Name => "Digestive";

        private float _accumulatedFrameTime;

        protected override void OnAdd()
        {
            Owner.EnsureComponent<StomachComponent>();
        }

        public override void OnRemove()
        {
            if (Owner.HasComponent<StomachComponent>())
            {
                Owner.RemoveComponent<StomachComponent>();
            }
        }

        public override void Update(float frameTime)
        {
            // Update at most once per second
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                Owner.GetComponent<StomachComponent>().Update(_accumulatedFrameTime);
                _accumulatedFrameTime = 0;
            }
        }
    }
}
