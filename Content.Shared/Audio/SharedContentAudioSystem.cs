using Content.Shared.Physics;

namespace Content.Shared.Audio;

public abstract class SharedContentAudioSystem : EntitySystem
{
    [Dependency] protected readonly SharedAudioSystem Audio = default!;

    /// <summary>
    /// Standard variation to use for sounds.
    /// </summary>
    public const float DefaultVariation = 0.05f;

    public override void Initialize()
    {
        base.Initialize();
        Audio.OcclusionCollisionMask = (int) CollisionGroup.Impassable;
    }
}
