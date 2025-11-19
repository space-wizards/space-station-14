using Content.Server.Destructible;
using Content.Shared.Construction;
using Content.Shared.Damage.Components;
using Content.Shared.Examine;
using Content.Shared.FixedPoint;

namespace Content.Server.Construction.Conditions;

/// <summary>
/// Requires that the structure has at least some amount of health
/// </summary>
[DataDefinition]
public sealed partial class MinHealth : IGraphCondition
{
    /// <summary>
    /// If ByProportion is true, Threshold is a value less than or equal to 1, but more than 0,
    /// which is compared to the percent of health remaining in the structure.
    /// Else, Threshold is any positive value with at most 2 decimal points of percision,
    /// which is compared to the current health of the structure.
    /// </summary>
    [DataField]
    public FixedPoint2 Threshold = 1;
    [DataField]
    public bool ByProportion = false;

    [DataField]
    public bool IncludeEquals = true;

    public bool Condition(EntityUid uid, IEntityManager entMan)
    {
        if (!entMan.TryGetComponent(uid, out DestructibleComponent? destructibleComp) ||
            !entMan.TryGetComponent(uid, out DamageableComponent? damageComp))
        {
            return false;
        }

        var destructionSys = entMan.System<DestructibleSystem>();
        var maxHealth = destructionSys.DestroyedAt(uid, destructibleComp);
        var curHealth = maxHealth - damageComp.TotalDamage;
        var proportionHealth = curHealth / maxHealth;

        if (IncludeEquals)
        {
            if (ByProportion)
            {
                return proportionHealth >= Threshold;
            }
            else
            {
                return curHealth >= Threshold;
            }
        }
        else
        {
            if (ByProportion)
            {
                return proportionHealth > Threshold;
            }
            else
            {
                return curHealth > Threshold;
            }
        }
    }

    public bool DoExamine(ExaminedEvent args)
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var entity = args.Examined;

        if (Condition(entity, entMan))
        {
            return false;
        }
        args.PushMarkup(Loc.GetString("construction-examine-condition-low-health"));

        return true;
    }

    public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
    {
        yield return new ConstructionGuideEntry()
        {
            Localization = "construction-step-condition-low-health"
        };
    }
}
