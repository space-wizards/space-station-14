using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed class ShowJobIconsSystem : EquipmentHudSystem<ShowJobIconsComponent>
{
    public void TryShowIcon(JobIconPrototype iconPrototype, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;
        ev.StatusIcons.Add(iconPrototype);
    }
}
