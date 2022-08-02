using Content.Client.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems.Body;
using Robust.Shared.GameStates;

namespace Content.Client.Body.Systems;

public sealed class BodySystem : SharedBodySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, ComponentHandleState>(OnComponentHandleState);
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
}
