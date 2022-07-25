using Content.Shared.Body.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Systems.Body;

public abstract partial class SharedBodySystem
{
    public void InitializeNetworking()
    {
        SubscribeLocalEvent<SharedBodyComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<SharedBodyComponent, ComponentHandleState>(OnComponentHandleState);
    }

    public void OnComponentGetState(EntityUid uid, SharedBodyComponent body, ref ComponentGetState args)
    {
        var partIds = new (string slot, EntityUid partId)[body.Parts.Count];

        var i = 0;
        foreach (var (part, slot) in body.Parts)
        {
            partIds[i] = (slot.Id, part.Owner);
            i++;
        }

        var parts = new Dictionary<string, EntityUid>(partIds.Length);

        foreach (var (slot, partId) in partIds)
        {
            if (Deleted(partId))
            {
                continue;
            }

            if (!HasComp<SharedBodyPartComponent>(partId))
            {
                continue;
            }

            parts[slot] = partId;
        }

        args.State = new BodyComponentState(parts);
    }

    public void OnComponentHandleState(EntityUid uid, SharedBodyComponent body, ref ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
        {
            return;
        }

        var newParts = state.Parts;

        foreach (var (oldPart, slot) in body.Parts)
        {
            if (!newParts.TryGetValue(slot.Id, out var newPart) ||
                newPart != oldPart.Owner)
            {
                RemovePart(uid, oldPart, body);
            }
        }

        foreach (var (slotId, newPart) in newParts)
        {
            if (!body.SlotIds.TryGetValue(slotId, out var slot) ||
                slot.Part?.Owner != newPart)
            {
                if (TryComp<SharedBodyPartComponent>(newPart, out var comp))
                {
                    AddPart(uid, slotId, comp, body);
                }
            }
        }
    }
}
