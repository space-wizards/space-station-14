using Content.Shared._Offbrand.Wounds;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ModifyBrainOxygen : EntityEffectBase<ModifyBrainOxygen>
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Amount > FixedPoint2.Zero)
            return Loc.GetString("entity-effect-guidebook-modify-brain-oxygen-heals", ("chance", Probability), ("amount", Amount));
        else
            return Loc.GetString("entity-effect-guidebook-modify-brain-oxygen-deals", ("chance", Probability), ("amount", -Amount));
    }
}

public sealed class ModifyBrainOxygenEntityEffectSystem : EntityEffectSystem<BrainDamageComponent, ModifyBrainOxygen>
{
    [Dependency] private readonly BrainDamageSystem _brainDamage = default!;

    protected override void Effect(Entity<BrainDamageComponent> ent, ref EntityEffectEvent<ModifyBrainOxygen> args)
    {
        _brainDamage.TryChangeBrainOxygenation(ent, args.Effect.Amount * args.Scale);
    }
}
