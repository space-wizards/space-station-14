using Content.Shared.Physics;

namespace Content.Shared.Audio;

public abstract class SharedContentAudioSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();
        _audio.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
    }
}
