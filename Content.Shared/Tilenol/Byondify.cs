using Content.Shared.Chemistry.Reagent;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared.Tilenol;

public sealed partial class Byondify : ReagentEffect
{
    [DataField]
    public float Strength = 5f;

    public static ProtoId<StatusEffectPrototype> LagKey = "Latency";
    public static ProtoId<StatusEffectPrototype> TileKey = "Tilenol";

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
        => Loc.GetString("reagent-effect-guidebook-byondify");

    public override void Effect(ReagentEffectArgs args)
    {
        var time = TimeSpan.FromSeconds(Strength *= args.Scale);
        var sys = args.EntityManager.EntitySysManager.GetEntitySystem<StatusEffectsSystem>();
        sys.TryAddStatusEffect<TilenolComponent>(args.SolutionEntity, TileKey, time, false);
        sys.TryAddStatusEffect<ByondComponent>(args.SolutionEntity, LagKey, time, false);
    }
}
