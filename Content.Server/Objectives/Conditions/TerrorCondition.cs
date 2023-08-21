using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Objective condition that requires the player to be a ninja and have called in a threat.
/// </summary>
[DataDefinition]
public sealed class TerrorCondition : IObjectiveCondition
{
    private Mind.Mind? _mind;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        return new TerrorCondition {_mind = mind};
    }

    public string Title => Loc.GetString("objective-condition-terror-title");

    public string Description => Loc.GetString("objective-condition-terror-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Objects/Fun/Instruments/otherinstruments.rsi"), "red_phone");

    public float Progress
    {
        get
        {
            var entMan = IoCManager.Resolve<EntityManager>();
            var mindSystem = entMan.System<MindSystem>();
            if (!mindSystem.TryGetRole<NinjaRole>(_mind, out var role))
                return 0f;

            return role.CalledInThreat ? 1f : 0f;
        }
    }

    public float Difficulty => 2.75f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is TerrorCondition cond && Equals(_mind, cond._mind);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is TerrorCondition cond && cond.Equals(this);
    }

    public override int GetHashCode()
    {
        return _mind?.GetHashCode() ?? 0;
    }
}
