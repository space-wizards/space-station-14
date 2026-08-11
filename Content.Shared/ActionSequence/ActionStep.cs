namespace Content.Shared.ActionSequence;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ActionStep
{
    [DataField]
    public string UserKey = ActionSequenceSystem.ActionStepUserKey;

    [DataField]
    public string TargetKey = ActionSequenceSystem.ActionStepTargetKey;

    [DataField]
    public string ActionKey = ActionSequenceSystem.ActionStepActionKey;

    public abstract void RaiseEvent(EntityUid target, IActionStepRaiser raiser);
}

public abstract partial class ActionStepBase<T> : ActionStep where T : ActionStepBase<T>
{
    public override void RaiseEvent(EntityUid target, IActionStepRaiser raiser)
    {
        if (this is not T type)
            return;

        raiser.RaiseEffectEvent(target, type);
    }
}

public interface IActionStepRaiser
{
    void RaiseEffectEvent<T>(EntityUid target, T effect) where T : ActionStepBase<T>;
}
