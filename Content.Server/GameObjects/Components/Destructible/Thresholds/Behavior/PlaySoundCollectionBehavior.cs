using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behavior
{
    public class PlaySoundCollectionBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Sound collection from which to pick a random sound to play.
        /// </summary>
        private string SoundCollection { get; set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.SoundCollection, "soundCollection", string.Empty);
        }

        public void Trigger(IEntity owner, DestructibleSystem system)
        {
            if (string.IsNullOrEmpty(SoundCollection))
            {
                return;
            }

            var sound = AudioHelpers.GetRandomFileFromSoundCollection(SoundCollection);
            var pos = owner.Transform.Coordinates;

            system.AudioSystem.PlayAtCoords(sound, pos, AudioHelpers.WithVariation(0.125f));
        }
    }
}
