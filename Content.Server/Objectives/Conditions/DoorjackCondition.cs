using Content.Server.Objectives.Interfaces;
using Content.Shared.Ninja.Components;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions;

[DataDefinition]
public sealed class DoorjackCondition : IObjectiveCondition
{
    private Mind.Mind? _mind;
    private int _target;

    public IObjectiveCondition GetAssigned(Mind.Mind mind)
    {
        // TODO: clamp to number of doors on station incase its somehow a shittle or something
        return new DoorjackCondition {
            _mind = mind,
            _target = IoCManager.Resolve<IRobustRandom>().Next(15, 40)
        };
    }

    public string Title => Loc.GetString("objective-condition-doorjack-title", ("count", _target));

    public string Description => Loc.GetString("objective-condition-doorjack-description", ("count", _target));

    public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Tools/emag.rsi"), "icon");

    public float Progress
    {
        get
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            if (_mind?.OwnedEntity == null
                || !entMan.TryGetComponent<NinjaComponent>(_mind.OwnedEntity, out var ninja))
                return 0f;

            // prevent divide-by-zero
            if (_target == 0)
                return 1f;

            return (float) ninja.DoorsJacked / (float) _target;
        }
    }

    public float Difficulty => 1.5f;

    public bool Equals(IObjectiveCondition? other)
    {
        return other is DoorjackCondition cond && Equals(_mind, cond._mind) && _target == cond._target;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj is DoorjackCondition cond && cond.Equals(this);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_mind?.GetHashCode() ?? 0, _target);
    }
}
