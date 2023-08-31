using Content.Server.Roles;
using Content.Server.Warps;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Objective condition that requires the player to be a ninja and have detonated their spider charge.
/// </summary>
[DataDefinition]
public sealed partial class SpiderChargeCondition : IObjectiveCondition
{
    private EntityUid? _mind;

    public IObjectiveCondition GetAssigned(EntityUid uid, MindComponent mind)
    {
        return new SpiderChargeCondition {
            _mind = uid
        };
    }

    public string Title
    {
        get
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent<NinjaRoleComponent>(_mind, out var role)
                || role.SpiderChargeTarget == null
                || !entMan.TryGetComponent<WarpPointComponent>(role.SpiderChargeTarget, out var warp)
                || warp.Location == null)
                // this should never really happen but eh
                return Loc.GetString("objective-condition-spider-charge-no-target");

            return Loc.GetString("objective-condition-spider-charge-title", ("location", warp.Location));
        }
    }

    public string Description => Loc.GetString("objective-condition-spider-charge-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Bombs/spidercharge.rsi"), "icon");

    public float Progress
    {
        get
        {
            var entMan = IoCManager.Resolve<EntityManager>();
            if (!entMan.TryGetComponent<NinjaRoleComponent>(_mind, out var role))
                return 0f;

            return role.SpiderChargeDetonated ? 1f : 0f;
        }
    }

    public float Difficulty => 2.5f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is SpiderChargeCondition cond && Equals(_mind, cond._mind);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is SpiderChargeCondition cond && cond.Equals(this);
    }

    public override int GetHashCode()
    {
        return _mind?.GetHashCode() ?? 0;
    }
}
