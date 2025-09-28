using System.Linq;
using Content.Shared.Medical.Disease;
using Content.Shared.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowDiseaseIconsSystem : EquipmentHudSystem<ShowDiseaseIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DiseaseCarrierComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(EntityUid uid, DiseaseCarrierComponent carrier, ref GetStatusIconsEvent ev)
    {
        if (!IsActive)
            return;

        if (carrier.ActiveDiseases.Count == 0)
            return;

        var iconId = carrier.DiseaseIcon;
        if (string.IsNullOrEmpty(iconId))
            return;

        if (_prototype.Resolve(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
    }
}
