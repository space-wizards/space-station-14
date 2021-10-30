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
        [DataField("sound", required: true)] public SoundSpecifier Sound { get; private set; } = default!;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            SoundSystem.Play(Filter.Pvs(uid), Sound.GetSound(), uid, AudioHelpers.WithVariation(0.125f));
        }
    }
}
