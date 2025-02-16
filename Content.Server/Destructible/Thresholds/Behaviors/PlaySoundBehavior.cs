using Content.Shared.Audio;
using Content.Shared.Destructible.Thresholds.Behaviors;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public sealed partial class PlaySoundBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [DataField(required: true)] public SoundSpecifier Sound;

        public void Execute(EntityUid owner,
            IDependencyCollection collection,
            EntityManager entManager,
            EntityUid? cause = null)
        {
            var pos = entManager.GetComponent<TransformComponent>(owner).Coordinates;
            entManager.System<SharedAudioSystem>().PlayPvs(Sound, pos);
        }
    }
}
