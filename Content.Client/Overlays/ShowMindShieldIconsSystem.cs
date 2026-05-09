using Content.Shared.Mindshield;
using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class ShowMindShieldIconsSystem : EquipmentHudSystem<ShowMindShieldIconsComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<StatusIconComponent> entt, ref GetStatusIconsEvent evnt)
    {
        // Is active checks for our ability to display status icons
        if (!IsActive)
            return;
        
        var ev = new QueryMindShieldVisualsEvent();
        RaiseLocalEvent(entt.Owner, ref ev, true);
        if (ev.IsVisible && _prototype.Resolve(ev.MindShieldStatusIcon, out var statusIconPrototype))
            evnt.StatusIcons.Add(statusIconPrototype);
    }
}
