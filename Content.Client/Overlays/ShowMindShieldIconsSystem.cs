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

    private void OnGetStatusIconsEvent(Entity<StatusIconComponent> ent, ref GetStatusIconsEvent args)
    {
        // Is active checks for our ability to display status icons
        if (!IsActive)
            return;

        _mindShieldSystem.GetMindshieldStatus(ent.Owner, out var _, out var isVisible);
        if (isVisible && _prototype.Resolve(SharedMindShieldSystem.StatusIcon, out var statusIconPrototype))
            args.StatusIcons.Add(statusIconPrototype);
    }
}
