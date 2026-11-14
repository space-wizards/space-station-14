using Content.Shared.Overlays;
using Content.Shared.Security.Components;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed partial class ShowCriminalRecordIconsSystem : EquipmentHudSystem<ShowCriminalRecordIconsComponent>
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CriminalRecordComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, CriminalRecordComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (ProtoMan.Resolve(component.StatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
