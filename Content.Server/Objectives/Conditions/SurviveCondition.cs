using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Just requires that the player is not dead, ignores evac and what not.
/// </summary>
[DataDefinition]
public sealed partial class SurviveCondition : IObjectiveCondition
{
    private EntityUid? _mind;

    public IObjectiveCondition GetAssigned(EntityUid uid, MindComponent mind)
    {
        return new SurviveCondition {_mind = uid};
    }

    public string Title => Loc.GetString("objective-condition-survive-title");

    public string Description => Loc.GetString("objective-condition-survive-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Clothing/Mask/ninja.rsi"), "icon");

    public float Difficulty => 0.5f;

    public float Progress
    {
        get
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent<MindComponent>(_mind, out var mind))
                return 0f;

            var mindSystem = entMan.System<SharedMindSystem>();
            return mindSystem.IsCharacterDeadIc(mind) ? 0f : 1f;
        }
    }

    public bool Equals(IObjectiveCondition? other)
    {
        return other is SurviveCondition condition && Equals(_mind, condition._mind);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((SurviveCondition) obj);
    }

    public override int GetHashCode()
    {
        return (_mind != null ? _mind.GetHashCode() : 0);
    }
}
