using Content.Shared.Roles;
using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;

namespace Content.Server.Jobs;

/// <summary>
/// Adds permanent status effects to the entity.
/// TODO: Move this, and other JobSpecials, from Server to Shared.
/// </summary>
[UsedImplicitly]
public sealed partial class ApplyStatusEffectSpecial : JobSpecial
{
    [DataField(required: true)]
    public HashSet<EntProtoId> StatusEffects { get; private set; } = new();

    public override void AfterEquip(EntityUid mob)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var statusSystem = entMan.System<StatusEffectsSystem>();
        foreach (var effect in StatusEffects)
        {
            statusSystem.TrySetStatusEffectDuration(mob, effect);
        }
    }
}
