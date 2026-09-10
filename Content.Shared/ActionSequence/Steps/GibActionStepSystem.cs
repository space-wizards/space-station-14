using Content.Shared.Changeling.Components;
using Content.Shared.Changeling.Systems;
using Content.Shared.Gibbing;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="GibActionStep"/>.
/// </summary>
public sealed partial class GibActionStepSystem : ActionStepSystem<GibActionStep>
{
    [Dependency] private GibbingSystem _gib = default!;
    [Dependency] private DestructionResistanceSystem _resist = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<GibActionStep> args)
    {
        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target))
            return;

        if (args.Step.BypassResistance)
            _resist.SetEnabled(target, false);

        _gib.Gib(target);
    }
}

/// <summary>
/// Gibs the TargetKey, optionally turning off destruction resistance.
/// </summary>
public sealed partial class GibActionStep : ActionStepBase<GibActionStep>
{
    /// <summary>
    /// Whether we should bypass the resistance granted by <see cref="DestructionResistanceComponent"/>
    /// </summary>
    [DataField]
    public bool BypassResistance;
}
