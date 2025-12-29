using Content.Shared._Offbrand.Wounds;
using Content.Shared.Construction;
using Content.Shared.FixedPoint;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class ChangeLungDamage : IGraphAction
{
    [DataField(required: true)]
    public FixedPoint2 Amount;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        entityManager.System<LungDamageSystem>().TryModifyDamage(uid, Amount);
    }
}
