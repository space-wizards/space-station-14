using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Examine;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void InitializeStructure()
    {
        SubscribeLocalEvent<SalvageStructureComponent, ExaminedEvent>(OnStructureExamine);
    }

    private void OnStructureExamine(EntityUid uid, SalvageStructureComponent component, ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("salvage-expedition-structure-examine"));
    }

    public void SetupStructureMission()
    {

    }
}
