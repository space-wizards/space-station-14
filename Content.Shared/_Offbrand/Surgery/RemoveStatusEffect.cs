using Content.Shared.Construction;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.Surgery;

[DataDefinition]
public sealed partial class RemoveStatusEffect : IGraphAction
{
    [DataField(required: true)]
    public EntProtoId Effect;

    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        var statusEffects = entityManager.System<StatusEffectsSystem>();
        statusEffects.TryRemoveStatusEffect(uid, Effect);
    }
}
