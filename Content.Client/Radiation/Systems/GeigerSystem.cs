using Content.Client.Items;
using Content.Client.Radiation.UI;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;

namespace Content.Client.Radiation.Systems;

public sealed class GeigerSystem : SharedGeigerSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, AfterAutoHandleStateEvent>(OnHandleState);
        Subs.ItemStatus<GeigerComponent>(ent => ent.Comp.ShowControl ? new GeigerItemControl(ent) : null);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref AfterAutoHandleStateEvent args)
    {
        component.UiUpdateNeeded = true;
    }
}
