using Content.Client.Items;
using Content.Client.Radiation.UI;
using Content.Shared.Item;
using Content.Shared.Radiation.Components;
using Content.Shared.Radiation.Systems;
using Robust.Shared.Prototypes;

namespace Content.Client.Radiation.Systems;

public sealed partial class GeigerSystem : SharedGeigerSystem
{
    public static readonly ProtoId<ItemStatusPrototype> GeigerItemStatus = "Geiger";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GeigerComponent, AfterAutoHandleStateEvent>(OnHandleState);
        Subs.ItemStatus<GeigerComponent>(ent => ent.Comp.ShowControl ? new GeigerItemControl(ent) : null, GeigerItemStatus);
    }

    private void OnHandleState(EntityUid uid, GeigerComponent component, ref AfterAutoHandleStateEvent args)
    {
        component.UiUpdateNeeded = true;
    }
}
