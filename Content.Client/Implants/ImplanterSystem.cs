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
        Subs.ItemStatus<ImplanterComponent>(ent => new ImplanterStatusControl(ent));
    }

    private void OnHandleImplanterState(EntityUid uid, ImplanterComponent component, ref AfterAutoHandleStateEvent args)
    {
        component.UiUpdateNeeded = true;
    }
}
