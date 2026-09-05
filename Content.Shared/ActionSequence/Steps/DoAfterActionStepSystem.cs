using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;

namespace Content.Shared.ActionSequence.Steps;

/// <summary>
/// System handling <see cref="DoAfterActionStep"/>.
/// </summary>
public sealed partial class DoAfterActionStepSystem : ActionStepSystem<DoAfterActionStep>
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;

    protected override void Step(Entity<ActionSequenceComponent> action, ref ActionStepEvent<DoAfterActionStep> args)
    {
        if (!SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.UserKey, out var user))
            return;

        var doAfterTarget = user;

        if (SequenceSystem.TryGetBlackboardData<EntityUid>(action, args.Step.TargetKey, out var target))
            doAfterTarget = target;

        var doAfter = new DoAfterArgs(EntityManager, user, args.Step.Delay, new ActionSequenceDoAfterEvent(), action, doAfterTarget)
        {
            Hidden = args.Step.Hidden,
            NeedHand = args.Step.NeedHand,
            BreakOnHandChange = args.Step.BreakOnHandChange,
            BreakOnMove = args.Step.BreakOnMove,
            BreakOnWeightlessMove =  args.Step.BreakOnWeightlessMove,
            MovementThreshold = args.Step.MovementThreshold,
            DistanceThreshold = args.Step.DistanceThreshold,
            BreakOnDamage =  args.Step.BreakOnDamage,
            DamageThreshold = args.Step.DamageThreshold,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            args.Await = SequenceAwaiting.DoAfter;
            args.Handled = true;
        }
    }
}

/// <summary>
/// Starts a doAfter. The DoAfterEvent is raised on the action.
/// If TargetKey is specified, it will be the target of the doAfter. Otherwise, it is the user.
/// </summary>
public sealed partial class DoAfterActionStep : ActionStepBase<DoAfterActionStep>
{
    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Delay"/>
    /// </summary>
    [DataField]
    public TimeSpan Delay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.Hidden"/>
    /// </summary>
    [DataField]
    public bool Hidden;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.NeedHand"/>
    /// </summary>
    [DataField]
    public bool NeedHand;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnHandChange"/>
    /// </summary>
    [DataField]
    public bool BreakOnHandChange = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnMove"/>
    /// </summary>
    [DataField]
    public bool BreakOnMove;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnWeightlessMove"/>
    /// </summary>
    [DataField]
    public bool BreakOnWeightlessMove = true;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.MovementThreshold"/>
    /// </summary>
    [DataField]
    public float MovementThreshold = 0.3f;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.DistanceThreshold"/>
    /// </summary>
    [DataField]
    public float? DistanceThreshold;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.BreakOnDamage"/>
    /// </summary>
    [DataField]
    public bool BreakOnDamage;

    /// <summary>
    /// <inheritdoc cref="DoAfterArgs.DamageThreshold"/>
    /// </summary>
    [DataField]
    public FixedPoint2 DamageThreshold = 1;
}
