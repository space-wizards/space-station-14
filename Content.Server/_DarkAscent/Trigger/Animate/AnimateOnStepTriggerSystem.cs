using Content.Shared.StepTrigger.Components;
using Content.Shared.StepTrigger.Systems;
using Content.Shared._DarkAscent.Trigger.Animate;
using Robust.Shared.Player;

namespace Content.Server._DarkAscent.Trigger.Animate;

public sealed class AnimateOnStepTriggerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<StepTriggerComponent, StepTriggerAttemptEvent>(OnStep);
    }

    private void OnStep(EntityUid uid, StepTriggerComponent comp, ref StepTriggerAttemptEvent args)
    {
        if (!HasComp<AnimateOnStepTriggerComponent>(uid))
            return;

        // Play the animation for all nearby clients
        RaiseNetworkEvent(new AnimateOnStepTriggerEvent(GetNetEntity(uid)), Filter.Pvs(uid));
    }
}
