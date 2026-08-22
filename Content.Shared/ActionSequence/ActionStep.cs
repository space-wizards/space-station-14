using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.ActionSequence;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ActionStep
{
    /// <summary>
    /// The general key used for the user of the action.
    /// Added to the blackboard when the action is called.
    /// </summary>
    [DataField]
    public string UserKey = ActionSequenceSystem.ActionStepUserKey;

    /// <summary>
    /// The general key used for the target of the action.
    /// Added to the blackboard when the action is called.
    /// </summary>
    /// <remarks>
    /// This is not the target of the event. This is the target of the action.
    /// If the action has no target, this will not be added.
    /// </remarks>
    [DataField]
    public string TargetKey = ActionSequenceSystem.ActionStepTargetKey;

    /// <summary>
    /// The general key used for the action itself.
    /// Added to the blackboard when the action is called.
    /// </summary>
    [DataField]
    public string ActionKey = ActionSequenceSystem.ActionStepActionKey;

    /// <summary>
    /// The event that causes a sequence step to happen and take effect.
    /// This should NOT be called directly.
    /// </summary>
    public abstract void RaiseEvent(Entity<ActionSequenceComponent> action, IActionStepRaiser raiser);
}

/// <summary>
/// Used to store an <see cref="ActionStep"/> so it can be raised without losing its Type.
/// </summary>
/// <typeparam name="T">The <see cref="ActionStep"/> type we are raising.</typeparam>
public abstract partial class ActionStepBase<T> : ActionStep where T : ActionStepBase<T>
{
    public override void RaiseEvent(Entity<ActionSequenceComponent> action, IActionStepRaiser raiser)
    {
        if (this is not T type)
            return;

        raiser.RaiseStepEvent(action, type);
    }
}

/// <summary>
/// Used to raise an <see cref="ActionStepEvent{T}"/> without losing the Type of the step.
/// </summary>
public interface IActionStepRaiser
{
    void RaiseStepEvent<T>(Entity<ActionSequenceComponent> action, T effect) where T : ActionStepBase<T>;
}
