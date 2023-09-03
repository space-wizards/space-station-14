using Robust.Shared.Timing;

namespace Content.Shared.Emp;

public abstract partial class SharedEmpSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;

    protected const string EmpDisabledEffectPrototype = "EffectEmpDisabled";
}
