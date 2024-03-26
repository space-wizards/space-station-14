using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Medical.Wounding.Components;
using Content.Shared.Medical.Wounding.Events;
using Robust.Shared.Containers;

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
        //We don't need an insure where because we don't remove bodyparts during mapInit
        //(If we do in the future for some reason, add an ensure in here or things will break)
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

        //the ensure is needed because this gets called by partAddedToBodyEvent which is raised from bodypart's mapInit
        _containerSystem.EnsureContainer<Container>(woundableEnt, WoundableComponent.WoundableContainerId);

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
