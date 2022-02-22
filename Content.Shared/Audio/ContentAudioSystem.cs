using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;

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
