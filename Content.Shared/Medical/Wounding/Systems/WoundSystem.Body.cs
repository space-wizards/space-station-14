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
        //We don't need an insure where because we don't remove bodyparts during mapInit
        //(If we do in the future for some reason, add an ensure in here or things will break)
        woundableComp.Body = null;
        foreach (var wound in GetAllWounds(woundable))
        {
            var ev = new WoundRemovedFromBody(args.OldBody, woundable, wound);
            RaiseLocalEvent(wound, ref ev);
            RaiseLocalEvent(args.OldBody, ref ev);
        }
        Dirty(woundableEnt, woundableComp);
    }

    private void OnWoundableAddedToBody(EntityUid woundableEnt, WoundableComponent woundableComp, ref BodyPartAddedToBodyEvent args)
    {
        var woundable = new Entity<WoundableComponent>(woundableEnt, woundableComp);
        //the ensure is needed because this gets called by partAddedToBodyEvent which is raised from bodypart's mapInit
        _containerSystem.EnsureContainer<Container>(woundableEnt, WoundableComponent.WoundableContainerId);

        woundableComp.Body = args.Body;
        foreach (var wound in GetAllWounds(woundable))
        {
            var ev = new WoundAppliedToBody(args.Body, woundable, wound);
            RaiseLocalEvent(wound, ref ev);
            RaiseLocalEvent(args.Body, ref ev);
        }
        Dirty(woundableEnt, woundableComp);
    }
}
