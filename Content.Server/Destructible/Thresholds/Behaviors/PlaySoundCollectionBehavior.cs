using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    [DataDefinition]
    public class PlaySoundCollectionBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Sound collection from which to pick a random sound to play.
        /// </summary>
        [DataField("soundCollection")]
        private string SoundCollection { get; set; } = string.Empty;

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (string.IsNullOrEmpty(SoundCollection))
            {
                return;
            }

            var sound = AudioHelpers.GetRandomFileFromSoundCollection(SoundCollection);
            var pos = owner.Transform.Coordinates;

            SoundSystem.Play(Filter.Pvs(pos), sound, pos, AudioHelpers.WithVariation(0.125f));
        }
    }
}
