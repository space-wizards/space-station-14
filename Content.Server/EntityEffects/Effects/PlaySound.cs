using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.EntityEffects;
using Robust.Shared.Prototypes;

namespace Content.Server.EntityEffects.Effects;

/// <summary>
/// Effect that continuously emits sound.
/// </summary>
[UsedImplicitly]
[DataDefinition]
public sealed partial class PlaySound : EntityEffect
{
    [DataField(required: true)]
    public SoundSpecifier Sound;

    // JUSTIFICATION: This is purely cosmetic.
    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => null;

    public override void Effect(EntityEffectBaseArgs args)
    {
        args.EntityManager.EntitySysManager.GetEntitySystem<SharedAudioSystem>()
            .PlayPvs(Sound, args.TargetEntity);
    }
}
