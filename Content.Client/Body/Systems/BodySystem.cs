using Content.Client.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems.Body;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Client.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnComponentHandleState);
        SubscribeLocalEvent<BodyComponent, EntInsertedIntoContainerMessage>(OnInsertedIntoContainer);
        SubscribeLocalEvent<BodyComponent, EntRemovedFromContainerMessage>(OnRemovedFromContainer);
    }

    public void OnComponentHandleState(EntityUid uid, BodyComponent body, ref ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
            return;

        foreach (var slotId in body.Slots.Keys)
        {
            if (!state.Slots.ContainsKey(slotId))
                body.Slots.Remove(slotId);
        }

        foreach (var (serverKey, serverSlot) in state.Slots)
        {
            var newSlot = new BodyPartSlot(serverSlot);
            SetSlot(uid, serverKey, newSlot);
        }
    }

    // TODO BODY: This is a bit of a hack so the HumanoidAppearanceSystem will still get its events.
    // In the future the body visuals should probably come from parts directly rather than being "casted" into HumanoidAppearanceSystem
    private void OnInsertedIntoContainer(EntityUid uid, BodyComponent body, EntInsertedIntoContainerMessage args)
    {
        if (!TryComp<SharedBodyPartComponent>(args.Entity, out var part))
            return;

        var ev = new PartAddedToBodyEvent(uid, part.Owner, args.Container.ID);
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(part.Owner, ev);
    }

    private void OnRemovedFromContainer(EntityUid uid, BodyComponent body, EntRemovedFromContainerMessage args)
    {
        if (!TryComp<SharedBodyPartComponent>(args.Entity, out var part))
            return;

        var ev = new PartRemovedFromBodyEvent(uid, part.Owner, args.Container.ID);
        RaiseLocalEvent(uid, ev);
        RaiseLocalEvent(args.Entity, ev);
    }
}
