using Content.Client.Implants.UI;
using Content.Client.Items;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;

namespace Content.Client.Implants;

public sealed class ImplanterSystem : SharedImplanterSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplanterComponent, AfterAutoHandleStateEvent>(OnHandleImplanterState);
        SubscribeLocalEvent<ImplanterComponent, ItemStatusCollectMessage>(OnItemImplanterStatus);
    }

    private void OnHandleImplanterState(EntityUid uid, ImplanterComponent component, ref AfterAutoHandleStateEvent args)
    {
        component.UiUpdateNeeded = true;
    }

    private void OnItemImplanterStatus(EntityUid uid, ImplanterComponent component, ItemStatusCollectMessage args)
    {
        args.Controls.Add(new ImplanterStatusControl(component));
    }
}
