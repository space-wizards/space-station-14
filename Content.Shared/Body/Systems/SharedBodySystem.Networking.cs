using System.Collections.Generic;
using Content.Shared.Body.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBodySystem
{
    public void InitializeNetworking()
    {
        SubscribeLocalEvent<SharedBodyComponent, ComponentGetState>(OnComponentGetState);
        SubscribeLocalEvent<SharedBodyComponent, ComponentHandleState>(OnComponentHandleState);
    }

    public void OnComponentGetState(EntityUid uid, SharedBodyComponent body, ComponentGetState args)
    {
        var partIds = new (string slot, EntityUid partId)[body.Parts.Count];

        var i = 0;
        foreach (var (part, slot) in body.Parts)
        {
            partIds[i] = (slot.Id, part.Owner);
            i++;
        }

        var parts = new Dictionary<string, SharedBodyPartComponent>(partIds.Length);

        foreach (var (slot, partId) in partIds)
        {
            if (Deleted(partId))
            {
                continue;
            }

            if (!TryComp(partId, out SharedBodyPartComponent? part))
            {
                continue;
            }

            parts[slot] = part;
        }

        args.State = new BodyComponentState(parts);
    }

    public void OnComponentHandleState(EntityUid uid, SharedBodyComponent body, ComponentHandleState args)
    {
        if (args.Current is not BodyComponentState state)
        {
            return;
        }

        var newParts = state.Parts;

        foreach (var (oldPart, slot) in body.Parts)
        {
            if (!newParts.TryGetValue(slot.Id, out var newPart) ||
                newPart != oldPart)
            {
                RemovePart(uid, oldPart, body);
            }
        }

        foreach (var (slotId, newPart) in newParts)
        {
            if (!body.SlotIds.TryGetValue(slotId, out var slot) ||
                slot.Part != newPart)
            {
                AddPart(uid, slotId, newPart, body);
            }
        }
    }
}
