using System;
using Content.Shared.Audio;
using Content.Shared.Sound;
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
        [DataField("sound", required: true)] public SoundSpecifier Sound { get; set; } = default!;

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            var pos = owner.Transform.Coordinates;
            SoundSystem.Play(Filter.Pvs(pos), Sound.GetSound(), pos, AudioHelpers.WithVariation(0.125f));
        }
    }
}
