using Content.Server.Objectives.Interfaces;
using Content.Server.Warps;
using Content.Shared.Ninja.Components;
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
            if (_mind?.OwnedEntity == null
                || !entMan.TryGetComponent<NinjaComponent>(_mind.OwnedEntity, out var ninja)
                || ninja.SpiderChargeTarget == null
                || !entMan.TryGetComponent<WarpPointComponent>(ninja.SpiderChargeTarget, out var warp)
                || warp.Location == null)
                // if you are funny and microbomb then press c, you get this
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
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (_mind?.OwnedEntity == null
                || !entMan.TryGetComponent<NinjaComponent>(_mind.OwnedEntity, out var ninja))
                return 0f;

            return ninja.SpiderChargeDetonated ? 1f : 0f;
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
