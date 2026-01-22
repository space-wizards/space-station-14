using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Destructible.Thresholds.Behaviors;

[Serializable]
[DataDefinition]
public sealed partial class PlaySoundBehavior : EntitySystem, IThresholdBehavior
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    /// <summary>
    ///     Sound played upon destruction.
    /// </summary>
    [DataField(required: true)]
    public SoundSpecifier Sound { get; set; } = default!;

    public void Execute(EntityUid owner, EntityUid? cause = null)
    {
        var pos = EntityManager.GetComponent<TransformComponent>(owner).Coordinates;
        _audio.PlayPvs(Sound, pos);
    }
}
