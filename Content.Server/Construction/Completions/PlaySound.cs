using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.Construction;
using Robust.Server.GameObjects.EntitySystems;
using Robust.Shared.GameObjects.Systems;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.Construction.Completions
{
    public class PlaySound : IEdgeCompleted, IStepCompleted
    {
        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Sound, "sound", string.Empty);
            serializer.DataField(this, x => x.SoundCollection, "soundCollection", string.Empty);
        }

        public string SoundCollection { get; private set; }
        public string Sound { get; private set; }

        public async Task StepCompleted(IEntity entity, IEntity user)
        {
            await Completed(entity, user);
        }

        public async Task Completed(IEntity entity, IEntity user)
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
