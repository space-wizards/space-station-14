using Content.Client.Overlays;
using Content.Shared._Goobstation.FakeMindshield.Components;
using Content.Shared._Goobstation.Overlays;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client._Goobstation.Overlays;

public sealed class ShowFakeMindShieldIconsSystem : EquipmentHudSystem<ShowFakeMindShieldIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FakeMindShieldComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(Entity<FakeMindShieldComponent> ent, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (_prototype.TryIndex(ent.Comp.MindShieldStatusIcon, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
