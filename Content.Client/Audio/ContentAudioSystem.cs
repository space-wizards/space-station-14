using Content.Shared.Physics;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client.Audio
{
    public sealed class ContentAudioSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            Get<AudioSystem>().OcclusionCollisionMask = (int) CollisionGroup.Impassable;
        }
    }
}
