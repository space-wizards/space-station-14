using Content.Shared.Mindshield;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed partial class ShowMindShieldIconsSystem : EquipmentHudSystem<ShowMindShieldIconsComponent>
{
    [Dependency] private MindShieldSystem _mindShield = default!;

    [SubscribeLocalEvent]
    private void OnGetStatusIconsEvent(Entity<StatusIconComponent> ent, ref GetStatusIconsEvent args)
    {
        // Is active checks for our ability to display status icons
        if (!IsActive)
            return;

        _mindShield.GetMindshieldStatus(ent.Owner, out _, out var isVisible);
        if (isVisible && ProtoMan.Resolve(MindShieldSystem.StatusIcon, out var statusIconPrototype))
            args.StatusIcons.Add(statusIconPrototype);
    }
}
