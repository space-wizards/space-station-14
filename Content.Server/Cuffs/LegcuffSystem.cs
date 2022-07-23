using Content.Server.Cuffs.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Interaction;

namespace Content.Server.Cuffs;

public sealed class LegcuffSystem : EntitySystem
{
    //TODO: AfterInteractionEvent is for testing purposes only, needs to be reworked into a rightclick verb
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LegcuffComponent, AfterInteractEvent>(OnAfterInteraction);
    }

    private void OnAfterInteraction(EntityUid uid, LegcuffComponent component, AfterInteractEvent args)
    {
        //TODO: This small bit works. Now expand it and make sure your movespeed goes back to normal
        if (!TryComp<LegCuffableComponent>(args.Target, out var legCuffable))
            return;

        legCuffable.IsCuffed = true;

        var ev = new LegcuffChangeEvent();
        RaiseLocalEvent(legCuffable.Owner, ev, false);
    }
}
