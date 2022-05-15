using Content.Shared.Physics;
using Robust.Shared.Audio;

namespace Content.Shared.Audio
{
    public sealed class ContentAudioSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();
            SoundSystem.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
        }
    }
}
