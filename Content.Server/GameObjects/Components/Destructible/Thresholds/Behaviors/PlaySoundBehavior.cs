using System;
using Content.Server.GameObjects.EntitySystems;
using Content.Shared.Audio;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Destructible.Thresholds.Behaviors
{
    [Serializable]
    public class PlaySoundBehavior : IThresholdBehavior
    {
        /// <summary>
        ///     Sound played upon destruction.
        /// </summary>
        public string Sound { get; set; }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Sound, "sound", string.Empty);
        }

        public void Execute(IEntity owner, DestructibleSystem system)
        {
            if (string.IsNullOrEmpty(Sound))
            {
                return;
            }

            var pos = owner.Transform.Coordinates;

            system.AudioSystem.PlayAtCoords(Sound, pos, AudioHelpers.WithVariation(0.125f));
        }
    }
}
