using Content.Server.GameObjects.Components.Body.Circulatory;
using Content.Server.GameObjects.EntitySystems;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;

namespace Content.Server.Body.Network
{
    /// <summary>
    ///     Handles all metabolism for mobs.
    ///     All delivery methods eventually bring reagents to the bloodstream.
    ///     For example, injectors put reagents directly into the bloodstream,
    ///     and the stomach does with some delay.
    /// </summary>
    [UsedImplicitly]
    public class CirculatoryNetwork : BodyNetwork
    {
        public override string Name => "Circulatory";

        private float _accumulatedFrameTime;

        protected override void OnAdd()
        {
            Owner.EnsureComponent<BloodstreamComponent>();
        }

        public override void OnRemove()
        {
            if (Owner.HasComponent<BloodstreamComponent>())
            {
                Owner.RemoveComponent<BloodstreamComponent>();
            }
        }

        /// <summary>
        ///     Triggers metabolism of the reagents inside _internalSolution.
        ///     Called by <see cref="BodySystem.Update"/>
        /// </summary>
        /// <param name="frameTime">The time since the last metabolism tick in seconds.</param>
        public override void Update(float frameTime)
        {
            // Update at most once per second
            _accumulatedFrameTime += frameTime;

            if (_accumulatedFrameTime > 1)
            {
                Owner.GetComponent<BloodstreamComponent>().Update(_accumulatedFrameTime);
                _accumulatedFrameTime = 0;
            }
        }
    }
}
