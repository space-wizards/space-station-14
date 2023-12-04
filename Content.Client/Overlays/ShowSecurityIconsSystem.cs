using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Mindshield.Components;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.SS220.CriminalRecords;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;
public sealed class ShowSecurityIconsSystem : EquipmentHudSystem<ShowSecurityIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototypeMan = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string JobIconForNoId = "JobIconNoId";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    private void OnGetStatusIconsEvent(EntityUid uid, StatusIconComponent _, ref GetStatusIconsEvent @event)
    {
        if (!IsActive || @event.InContainer)
        {
            return;
        }

        var healthIcons = DecideSecurityIcon(uid);

        @event.StatusIcons.AddRange(healthIcons);
    }

    private IReadOnlyList<StatusIconPrototype> DecideSecurityIcon(EntityUid uid)
    {
        var result = new List<StatusIconPrototype>();

        var jobIconToGet = JobIconForNoId;
        string? securityRecordType = null; //SS220 Criminal-Records
        if (_accessReader.FindAccessItemsInventory(uid, out var items))
        {
            foreach (var item in items)
            {
                // ID Card
                if (TryComp(item, out IdCardComponent? id))
                {
                    jobIconToGet = id.JobIcon;
                    securityRecordType = id.CurrentSecurityRecord?.RecordType; //SS220 Criminal-Records
                    break;
                }

                // PDA
                if (TryComp(item, out PdaComponent? pda)
                    && pda.ContainedId != null
                    && TryComp(pda.ContainedId, out id))
                {
                    jobIconToGet = id.JobIcon;
                    securityRecordType = id.CurrentSecurityRecord?.RecordType; //SS220 Criminal-Records
                    break;
                }
            }
        }

        if (_prototypeMan.TryIndex<StatusIconPrototype>(jobIconToGet, out var jobIcon))
            result.Add(jobIcon);
        else
            Log.Error($"Invalid job icon prototype: {jobIcon}");

        //SS220 Criminal-Records begin
        if (securityRecordType != null)
        {
            if (_prototypeMan.TryIndex<CriminalStatusPrototype>(securityRecordType, out var criminalStatus))
            {
                if (criminalStatus.StatusIcon.HasValue)
                {
                    if (_prototypeMan.TryIndex<StatusIconPrototype>(criminalStatus.StatusIcon, out var secIcon))
                        result.Add(secIcon);
                    else
                        Log.Error($"Invalid security status icon prototype: {secIcon}");
                }
            }
            else
            {
                Log.Error($"Invalid security status prototype: {criminalStatus}");
            }
        }
        //SS220 Criminal-Records end

        if (TryComp<MindShieldComponent>(uid, out var comp))
        {
            if (_prototypeMan.TryIndex<StatusIconPrototype>(comp.MindShieldStatusIcon.Id, out var icon))
                result.Add(icon);
        }

        // Add arrest icons here, WYCI.

        return result;
    }
}
