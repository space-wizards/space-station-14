using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

/// <summary>
/// Objective condition that requires the player to be a ninja and have stolen at least a random number of technologies.
/// </summary>
[DataDefinition]
public sealed class StealResearchCondition : IObjectiveCondition
{
    private Mind.Mind? _mind;
    private int _target;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        // TODO: clamp to number of research nodes in a single discipline maybe so easily maintainable
        return new DownloadCondition {
            _mind = mind,
            _target = IoCManager.Resolve<IRobustRandom>().Next(5, 10)
        };
    }

    public string Title => Loc.GetString("objective-condition-download-title", ("count", _target));

    public string Description => Loc.GetString("objective-condition-download-description");

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Structures/Machines/server.rsi"), "server");

    public float Progress
    {
        get
        {
            // prevent divide-by-zero
            if (_target == 0)
                return 1f;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var mindSystem = entMan.System<MindSystem>();
            if (!mindSystem.TryGetRole<NinjaRole>(_mind, out var role))
                return 0f;

            if (role.DownloadedNodes.Count >= _target)
                return 1f;

            return (float) role.DownloadedNodes.Count / (float) _target;
        }
    }

    public float Difficulty => 2.5f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is DownloadCondition cond && Equals(_mind, cond._mind) && _target == cond._target;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is DownloadCondition cond && cond.Equals(this);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_mind?.GetHashCode() ?? 0, _target);
    }
}
