using Content.Shared.Physics;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Components;
using Robust.Shared.Audio.Systems;

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

    protected void SilenceAudio()
    {
        var query = AllEntityQuery<AudioComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            Audio.SetGain(uid, 0f, comp);
        }
    }
}
