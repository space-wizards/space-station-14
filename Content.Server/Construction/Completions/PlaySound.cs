#nullable enable
using System.Threading.Tasks;
using Content.Shared.Audio;
using Content.Shared.Construction;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public class PlaySound : IGraphAction
    {
        [DataField("sound")] public SoundSpecifier Sound { get; private set; } = default!;

        public async Task PerformAction(IEntity entity, IEntity? user)
        {
            if(Sound.TryGetSound(out var sound))
                SoundSystem.Play(Filter.Pvs(entity), sound, entity, AudioHelpers.WithVariation(0.125f));
        }
    }
}
