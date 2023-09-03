using Content.Shared.Physics;

namespace Content.Shared.Audio;

public abstract partial class SharedContentAudioSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;

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
