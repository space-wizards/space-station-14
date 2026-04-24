using Content.Shared._Offbrand.Organs;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body;
using Content.Shared.Construction;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class ChangeOrganDamage : IGraphAction
{
    [DataField(required: true)]
    public ProtoId<OrganCategoryPrototype> Category;

    [DataField(required: true)]
    public FixedPoint2 Amount;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        entityManager.System<BodySystem>()
            .TryGetOrgansWithCategoryAndComponent<DamageableOrganComponent>(uid,
                out var organs,
                Category);

        foreach (var organ in organs)
        {
            entityManager.System<DamageableOrganSystem>()
                .ChangeDamage((organ, organ), Amount);
        }
    }
}
