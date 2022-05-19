using Content.Shared.Construction;
using Content.Shared.Sound;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server.Construction.Completions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class PlaySound : IGraphAction
    {
        [DataField("sound", required: true)] public SoundSpecifier Sound { get; private set; } = default!;

        [DataField("AudioParams")]
        public AudioParams AudioParams = AudioParams.Default;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("variation")]
        public float Variation = 0.125f;

        public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
        {
            var scale = (float) IoCManager.Resolve<IRobustRandom>().NextGaussian(1, Variation);
            SoundSystem.Play(Filter.Pvs(uid, entityManager: entityManager), Sound.GetSound(), uid, AudioParams.WithPitchScale(scale));
        }
    }
}
