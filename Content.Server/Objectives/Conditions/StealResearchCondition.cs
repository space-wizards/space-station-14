using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Objective condition that requires the player to be a ninja and have stolen at least a random number of technologies.
/// </summary>
[DataDefinition]
public sealed partial class StealResearchCondition : IObjectiveCondition
{
    private EntityUid? _mind;
    private int _target;

    public IObjectiveCondition GetAssigned(EntityUid uid, MindComponent mind)
    {
        // TODO: clamp to number of research nodes in a single discipline maybe so easily maintainable
        return new StealResearchCondition {
            _mind = uid,
            _target = IoCManager.Resolve<IRobustRandom>().Next(5, 10)
        };
    }

    public string Title => Loc.GetString("objective-condition-steal-research-title", ("count", _target));

    public string Description => Loc.GetString("objective-condition-steal-research-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Structures/Machines/server.rsi"), "server");

    public float Progress
    {
        get
        {
            // prevent divide-by-zero
            if (_target == 0)
                return 1f;

            var entMan = IoCManager.Resolve<IEntityManager>();
            if (!entMan.TryGetComponent<NinjaRoleComponent>(_mind, out var role))
                return 0f;

            if (role.DownloadedNodes.Count >= _target)
                return 1f;

            return (float) role.DownloadedNodes.Count / (float) _target;
        }
    }

    public float Difficulty => 2.5f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is StealResearchCondition cond && Equals(_mind, cond._mind) && _target == cond._target;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is StealResearchCondition cond && cond.Equals(this);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_mind?.GetHashCode() ?? 0, _target);
    }
}
