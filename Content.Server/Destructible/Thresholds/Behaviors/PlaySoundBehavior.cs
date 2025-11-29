using Content.Shared.Destructible;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class PlaySoundBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [DataField("sound", required: true)] public SoundSpecifier Sound { get; set; } = default!;

        public void Execute(EntityUid owner, DestructibleBehaviorSystem system, EntityUid? cause = null)
        {
            var pos = system.EntityManager.GetComponent<TransformComponent>(owner).Coordinates;
            system.EntityManager.System<SharedAudioSystem>().PlayPvs(Sound, pos);
        }
    }
}
