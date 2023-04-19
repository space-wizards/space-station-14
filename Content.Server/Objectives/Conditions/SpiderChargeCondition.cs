using Content.Server.Ninja.Systems;
using Content.Server.Objectives.Interfaces;
using Content.Server.Warps;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed class SpiderChargeCondition : IObjectiveCondition
{
    private Mind.Mind? _mind;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        return new SpiderChargeCondition {
            _mind = mind
        };
    }

    public string Title
    {
        get
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!NinjaSystem.GetNinjaRole(_mind, out var role)
                || role.SpiderChargeTarget == null
                || !entMan.TryGetComponent<WarpPointComponent>(role.SpiderChargeTarget, out var warp)
                || warp.Location == null)
                // this should never really happen but eh
                return Loc.GetString("objective-condition-spider-charge-no-target");

            return Loc.GetString("objective-condition-spider-charge-title", ("location", warp.Location));
        }
    }

    public string Description => Loc.GetString("objective-condition-spider-charge-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Bombs/spidercharge.rsi"), "icon");

    public float Progress
    {
        get
        {
            if (!NinjaSystem.GetNinjaRole(_mind, out var role))
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
