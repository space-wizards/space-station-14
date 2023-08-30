using Content.Shared.Physics;

namespace Content.Shared.Audio;

public abstract class SharedContentAudioSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    /// Standard variation to use for sounds.
    /// </summary>
    public const float DefaultVariation = 0.05f;

    public override void Initialize()
    {
        base.Initialize();
        _audio.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
    }
}
