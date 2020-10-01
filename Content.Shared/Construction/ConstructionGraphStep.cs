using System;
using Content.Shared.Audio;
using Robust.Shared.Interfaces.Serialization;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared.Construction
{
    [Serializable, NetSerializable]
    public abstract class ConstructionGraphStep : IExposeData
    {
        public float DoAfter { get; private set; }
        public string Sound { get; private set; }
        public string SoundCollection { get; private set; }
        public string SpriteState { get; private set; }
        public string Popup { get; private set; }

        public virtual void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.DoAfter, "doAfter", 0f);
            serializer.DataField(this, x => x.Sound, "sound", string.Empty);
            serializer.DataField(this, x => x.SoundCollection, "soundCollection", string.Empty);
            serializer.DataField(this, x => x.SpriteState, "spriteState", string.Empty);
        }

        public abstract void DoExamine(FormattedMessage message, bool inDetailsRange);

        public string GetSound()
        {
            return !string.IsNullOrEmpty(SoundCollection) ? AudioHelpers.GetRandomFileFromSoundCollection(SoundCollection) : Sound;
        }
    }
}
