using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;

namespace Content.Server.Chemistry.ReagentEffects
{
    /// <summary>
    /// Effect for reagents that continuously emit sound.
    /// </summary>
	[UsedImplicitly]
    [DataDefinition]
    public sealed partial class PlaySound : ReagentEffect
    {
        [DataField(required: true)]
        public SoundSpecifier Sound;

        // JUSTIFICATION: This is purely cosmetic.
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
            => null;

        public override void Effect(ReagentEffectArgs args)
        {
            args.EntityManager.EntitySysManager.GetEntitySystem<SharedAudioSystem>()
                .PlayPvs(Sound, args.SolutionEntity);
        }
    }
}
