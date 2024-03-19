using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;

namespace Content.Shared.Medical.Wounding.Systems;

public sealed partial class WoundSystem
{

    private void InitBodyListeners()
    {
        SubscribeLocalEvent<WoundableComponent, BodyPartAddedToBodyEvent>(OnWoundableAddedToBody);
        SubscribeLocalEvent<WoundableComponent, BodyPartRemovedFromBodyEvent>(OnWoundableRemovedFromBody);
    }

    private void OnWoundableRemovedFromBody(EntityUid woundableEnt, WoundableComponent woundableComp, ref BodyPartRemovedFromBodyEvent args)
    {
        var woundable = new Entity<WoundableComponent>(woundableEnt, woundableComp);
        var body = new Entity<BodyComponent>(args.BodyUid, args.Body);
        woundableComp.Body = null;
        foreach (var wound in GetAllWounds(woundable))
        {
            var ev = new WoundRemovedFromBody(body, woundable, wound);
            RaiseLocalEvent(wound, ref ev);
            RaiseLocalEvent(body, ref ev);
        }
        Dirty(woundableEnt, woundableComp);
    }

    private void OnWoundableAddedToBody(EntityUid woundableEnt, WoundableComponent woundableComp, ref BodyPartAddedToBodyEvent args)
    {
        var woundable = new Entity<WoundableComponent>(woundableEnt, woundableComp);
        var body = new Entity<BodyComponent>(args.BodyUid, args.Body);
        woundableComp.Body = args.BodyUid;
        foreach (var wound in GetAllWounds(woundable))
        {
            var ev = new WoundAppliedToBody(body, woundable, wound);
            RaiseLocalEvent(wound, ref ev);
            RaiseLocalEvent(body, ref ev);
        }
        Dirty(woundableEnt, woundableComp);
    }
}
