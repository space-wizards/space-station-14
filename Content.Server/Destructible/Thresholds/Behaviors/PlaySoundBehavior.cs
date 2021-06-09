using System;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class PlaySoundBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        [DataField("sound")] public string Sound { get; set; } = string.Empty;

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (string.IsNullOrEmpty(Sound))
            {
                return;
            }

            var pos = owner.Transform.Coordinates;
            SoundSystem.Play(Filter.Pvs(pos), Sound, pos, AudioHelpers.WithVariation(0.125f));
        }
    }
}
