using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowMindShieldIconsSystem : EquipmentHudSystem<ShowMindShieldIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindShieldComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
        SubscribeLocalEvent<FakeMindShieldComponent, GetStatusIconsEvent>(OnGetStatusIconsEventFake);
    }
    // TODO: Probably need to get this OFF of client since this can be read by bad actors rather easily
    //  ...imagine cheating in a game about silly paper dolls
    private void OnGetStatusIconsEventFake(EntityUid uid, FakeMindShieldComponent component, ref GetStatusIconsEvent ev)
    {
        if(!IsActive)
            return;
        if (component.IsEnabled && _prototype.TryIndex(component.MindShieldStatusIcon, out var fakeStatusIconPrototype))
            ev.StatusIcons.Add(fakeStatusIconPrototype);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, MindShieldComponent component, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (_prototype.TryIndex(component.MindShieldStatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
