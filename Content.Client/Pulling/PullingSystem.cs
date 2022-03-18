using Content.Shared.Pulling;
using Content.Shared.Pulling.Components;
using JetBrains.Annotations;
using Robust.Client.Physics;

namespace Content.Client.Pulling
{
    [UsedImplicitly]
    public sealed class PullingSystem : SharedPullingSystem
    {
        public override void Initialize()
        {
            base.Initialize();

            UpdatesAfter.Add(typeof(PhysicsSystem));
        }
    }
}
