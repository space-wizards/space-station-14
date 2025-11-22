using Content.Shared._Offbrand.Wounds;
using Content.Shared.Construction;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class AddWound : IGraphAction
{
    [DataField(required: true)]
    public EntProtoId Wound;

    [DataField(required: true)]
    public Damages Damages;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (entityManager.TryGetComponent<WoundableComponent>(uid, out var woundable))
        {
            var woundableSystem = entityManager.System<WoundableSystem>();
            woundableSystem.TryWound((uid, woundable), Wound, Damages, refreshDamage: true);
        }
    }
}
