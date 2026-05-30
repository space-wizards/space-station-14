using Content.Shared.Mindshield;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed partial class ShowMindShieldIconsSystem : EquipmentHudSystem<ShowMindShieldIconsComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private SharedMindShieldSystem _mindShieldSystem = default!;

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

        _mindShieldSystem.GetMindshieldStatus(entt.Owner, out var _, out var isVisible, out var statusIcon);
        if (isVisible && _prototype.Resolve(statusIcon, out var statusIconPrototype))
            evnt.StatusIcons.Add(statusIconPrototype);
    }
}
