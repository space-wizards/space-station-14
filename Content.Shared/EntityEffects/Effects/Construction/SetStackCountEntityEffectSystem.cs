using Content.Shared.Stacks;

namespace Content.Shared.EntityEffects.Effects.Construction;

/// <summary>
/// Sets the stack count.
/// </summary>
public sealed partial class SetStackCountEntityEffectSystem : EntityEffectSystem<StackComponent, SetStackCount>
{
    [Dependency] private SharedStackSystem _stack = default!;

    protected override void Effect(Entity<StackComponent> entity, ref EntityEffectEvent<SetStackCount> args)
    {
        _stack.SetCount(entity.AsNullable(), args.Effect.Amount);
    }
}

public sealed partial class SetStackCount : EntityEffectBase<SetStackCount>
{
    [DataField]
    public int Amount = 1;
}
