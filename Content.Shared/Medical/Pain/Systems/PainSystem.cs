using Content.Shared.Body.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Medical.Pain.Components;
using Content.Shared.Medical.Wounds.Components;
using Content.Shared.Medical.Wounds.Systems;

namespace Content.Shared.Medical.Pain.Systems;

public sealed partial class PainSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<WoundableComponent,WoundAddedEvent>(OnWoundAdded);
        SubscribeLocalEvent<WoundableComponent,WoundRemovedEvent>(OnWoundRemoved);
    }

    public void ApplyPain(EntityUid target, PainReceiverComponent receiver, FixedPoint2 pain)
    {
        UpdateBasePain(target, receiver, receiver.BasePain + pain);
    }

    private void UpdatePain(EntityUid target, PainReceiverComponent receiver, FixedPoint2 newBasePain,
        FixedPoint2 newModifier, bool raiseEvents = false)
    {
        if (newModifier == receiver.Modifier && receiver.BasePain == newBasePain)
            return;
        var newPain = newBasePain * newModifier;
        receiver.Modifier = newModifier;
        receiver.Pain = newPain;
        if (!raiseEvents)
            return;
        var ev = new PainUpdatedEvent(newPain);
        RaiseLocalEvent(target, ref ev);
        if (receiver.Pain < receiver.Limit)
            return;
        var ev2 = new PainOverloadEvent();
        RaiseLocalEvent(target, ref ev2);
    }

    private void UpdateModifier(EntityUid target, PainReceiverComponent receiver, FixedPoint2 modifier,
        bool raiseEvent = true)
    {
        UpdatePain(target, receiver, receiver.BasePain, modifier, raiseEvent);
    }

    private void UpdateBasePain(EntityUid target, PainReceiverComponent receiver, FixedPoint2 newBasePain,
        bool raiseEvent = true)
    {
        UpdatePain(target, receiver, newBasePain, receiver.Modifier, raiseEvent);
    }
}
