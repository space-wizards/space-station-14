using Content.Shared._Offbrand.Organs;
using Content.Shared.Body;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.EntityEffects;

public sealed partial class ModifyOrganOxygen : EntityEffectBase<ModifyOrganOxygen>
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public override string? EntityEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        if (Amount < FixedPoint2.Zero)
            return Loc.GetString("entity-effect-guidebook-modify-lung-damage-heals", ("chance", Probability), ("amount", -Amount));
        else
            return Loc.GetString("entity-effect-guidebook-modify-lung-damage-deals", ("chance", Probability), ("amount", Amount));
    }
}

public sealed class ModifyOrganOxygenEntityEffectSystem : EntityEffectSystem<BodyComponent, ModifyOrganOxygen>
{
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly OxygenatableOrganSystem _organ = default!;

    protected override void Effect(Entity<BodyComponent> ent, ref EntityEffectEvent<ModifyOrganOxygen> args)
    {
        _body.TryGetOrgansWithCategoryAndComponent<OxygenatableOrganComponent>(
            ent.AsNullable(),
            out var organs,
            args.Effect.Category);

        foreach (var organ in organs)
        {
            _organ.ChangeOxygenation((organ, organ), args.Effect.Amount);
        }
    }
}
