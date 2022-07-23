using Content.Server.Ensnaring.Components;
using Content.Shared.Ensnaring.Components;
using Content.Shared.Interaction;

namespace Content.Server.Ensnaring;

public sealed class EnsnaringSystem : EntitySystem
{
    //TODO: AfterInteractionEvent is for testing purposes only, needs to be reworked into a rightclick verb
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EnsnaringComponent, AfterInteractEvent>(OnAfterInteraction);
    }

    private void OnAfterInteraction(EntityUid uid, EnsnaringComponent component, AfterInteractEvent args)
    {
        //TODO: This small bit works and works with speed.
        //Now I need nuance to store the cuffs on the person on cuff and remove them from storage on uncuff.
        //Also need to check for feet.
        if (!TryComp<EnsnareableComponent>(args.Target, out var legCuffable))
            return;

        legCuffable.IsEnsnared = !legCuffable.IsEnsnared;

        var ev = new EnsnareChangeEvent(component.WalkSpeed, component.SprintSpeed);
        RaiseLocalEvent(legCuffable.Owner, ev, false);
    }
}
