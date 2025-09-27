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

    private static readonly ProtoId<HealthIconPrototype> IllIconId = "DiseaseIconIll";
    private static readonly ProtoId<HealthIconPrototype> BuffIconId = "DiseaseIconBuff";

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

        var hasHarmful = false;
        var hasBeneficial = false;

        foreach (var (id, _) in carrier.ActiveDiseases.ToArray())
        {
            if (!_prototype.TryIndex<DiseasePrototype>(id, out var disease))
                continue;

            if (disease.IsBeneficial)
                hasBeneficial = true;
            else
                hasHarmful = true;
        }

        if (hasHarmful && _prototype.Resolve(IllIconId, out var illIcon))
            ev.StatusIcons.Add(illIcon);
        else if (hasBeneficial && _prototype.Resolve(BuffIconId, out var buffIcon))
            ev.StatusIcons.Add(buffIcon);
    }
}
