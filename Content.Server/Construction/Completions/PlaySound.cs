#nullable enable
using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    public class PlaySound : IGraphAction
    {
        public string SoundCollection { get; private set; } = string.Empty;
        public string Sound { get; private set; } = string.Empty;

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Sound, "sound", string.Empty);
            serializer.DataField(this, x => x.SoundCollection, "soundCollection", string.Empty);
        }

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            var sound = GetSound();

            if (string.IsNullOrEmpty(sound)) return;

            EntitySystem.Get<AudioSystem>().PlayFromEntity(sound, entity, AudioHelpers.WithVariation(0.125f));
        }

        private string GetSound()
        {
            return !string.IsNullOrEmpty(SoundCollection) ? AudioHelpers.GetRandomFileFromSoundCollection(SoundCollection) : Sound;
        }
    }
}
