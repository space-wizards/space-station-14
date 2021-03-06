#nullable enable
using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.Construction;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class PlaySound : IGraphAction
    {
        [DataField("soundCollection")] public string SoundCollection { get; private set; } = string.Empty;
        [DataField("sound")] public string Sound { get; private set; } = string.Empty;

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
