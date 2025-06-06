using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.Overlays;

public sealed class ShowJobIconsSystem : EquipmentHudSystem<ShowJobIconsComponent>
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ShowJobIconsComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<ShowJobIconsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        RefreshOverlay();
    }

    public void TryShowIcon(JobIconPrototype iconPrototype, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        ev.StatusIcons.Add(iconPrototype);
    }
}
