using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Inventory.Events;
using Content.Shared.Mindshield.Components;
using Content.Shared.NukeOps;
using Content.Shared.Nutrition.Components;
using Content.Shared.Overlays;
using Content.Shared.PDA;
using Content.Shared.Security.Components;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.Overlays;

public sealed class ShowStatusIconsSystem : EquipmentHudSystem<ShowStatusIconsComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;

    ////////////////////////////////////////////////////////
    // TODO: Convert all this to prototypes, somehow...
    [ValidatePrototypeId<StatusIconPrototype>]
    private const string JobIconNoId = "JobIconNoId";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HungerIconOverfedId = "HungerIconOverfed";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HungerIconPeckishId = "HungerIconPeckish";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string HungerIconStarvingId = "HungerIconStarving";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string ThirstIconOverhydratedId = "ThirstIconOverhydrated";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string ThirstIconThirstyId = "ThirstIconThirsty";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string ThirstIconParchedId = "ThirstIconParched";

    [ValidatePrototypeId<StatusIconPrototype>]
    private const string MindShieldIconId = "MindShieldIcon";

    [ValidatePrototypeId<StatusIconPrototype>]
    public const string NukeOperativeIconId = "SyndicateFaction";

    private StatusIconPrototype? _hungerIconOverfed = null;
    private StatusIconPrototype? _hungerIconPeckish = null;
    private StatusIconPrototype? _hungerIconStarving = null;
    private StatusIconPrototype? _thirstIconOverhydrated = null;
    private StatusIconPrototype? _thirstIconThirsty = null;
    private StatusIconPrototype? _thirstIconParched = null;
    private StatusIconPrototype? _mindShieldIcon = null;
    private StatusIconPrototype? _nukeOperativeIcon = null;
    ////////////////////////////////////////////////////////

    // TODO: should these be turned into a bitflag?
    private bool _showJob = false;
    private bool _showHunger = false;
    private bool _showThirst = false;
    private bool _showMindShield = false;
    private bool _showCriminalRecord = false;
    private bool _showNukeOperative = false;

    public override void Initialize()
    {
        base.Initialize();

        ////////////////////////////////////////////////////////
        // TODO: Convert all this to prototypes, somehow...
        _hungerIconOverfed = _prototype.Index<StatusIconPrototype>(HungerIconOverfedId);
        _hungerIconPeckish = _prototype.Index<StatusIconPrototype>(HungerIconPeckishId);
        _hungerIconStarving = _prototype.Index<StatusIconPrototype>(HungerIconStarvingId);
        _thirstIconOverhydrated = _prototype.Index<StatusIconPrototype>(ThirstIconOverhydratedId);
        _thirstIconThirsty = _prototype.Index<StatusIconPrototype>(ThirstIconThirstyId);
        _thirstIconParched = _prototype.Index<StatusIconPrototype>(ThirstIconParchedId);
        _mindShieldIcon = _prototype.Index<StatusIconPrototype>(MindShieldIconId);
        _nukeOperativeIcon = _prototype.Index<StatusIconPrototype>(NukeOperativeIconId);
        ////////////////////////////////////////////////////////

        SubscribeLocalEvent<StatusIconComponent, GetStatusIconsEvent>(OnGetStatusIconsEvent);
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<ShowStatusIconsComponent> ev)
    {
        base.UpdateInternal(ev);

        foreach (var showStatusIcon in ev.Components)
        {
            // TODO: should these be turned into a bitflag?
            _showJob |= showStatusIcon.ShowJob;
            _showHunger |= showStatusIcon.ShowHunger;
            _showThirst |= showStatusIcon.ShowThirst;
            _showMindShield |= showStatusIcon.ShowMindShield;
            _showCriminalRecord |= showStatusIcon.ShowCriminalRecord;
            _showNukeOperative |= showStatusIcon.ShowNukeOperative;
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        // TODO: should these be turned into a bitflag?
        _showJob = false;
        _showHunger = false;
        _showThirst = false;
        _showMindShield = false;
        _showCriminalRecord = false;
        _showNukeOperative = false;
    }

    private void OnGetStatusIconsEvent(EntityUid uid, StatusIconComponent statusIcon, ref GetStatusIconsEvent ev)
    {
        if (!IsActive || ev.InContainer)
            return;

        var result = new List<StatusIconPrototype>();

        if (_showJob)
        {
            var jobId = GetJobIconIdFromAccessItems(uid);

            if (_prototype.TryIndex<StatusIconPrototype>(jobId, out var jobIcon))
                result.Add(jobIcon);
            else
                Log.Error($"Invalid job icon prototype: {jobIcon}");
        }

        if (_showHunger && TryComp<HungerComponent>(uid, out var hunger))
        {
            if      (hunger.CurrentThreshold == HungerThreshold.Overfed)    result.Add(_hungerIconOverfed!);
            else if (hunger.CurrentThreshold == HungerThreshold.Peckish)    result.Add(_hungerIconPeckish!);
            else if (hunger.CurrentThreshold == HungerThreshold.Starving)   result.Add(_hungerIconStarving!);
        }

        if (_showThirst && TryComp<ThirstComponent>(uid, out var thirst))
        {
            if      (thirst.CurrentThirstThreshold == ThirstThreshold.OverHydrated) result.Add(_thirstIconOverhydrated!);
            else if (thirst.CurrentThirstThreshold == ThirstThreshold.Thirsty)      result.Add(_thirstIconThirsty!);
            else if (thirst.CurrentThirstThreshold == ThirstThreshold.Parched)      result.Add(_thirstIconParched!);
        }

        if (_showMindShield && HasComp<MindShieldComponent>(uid))
            result.Add(_mindShieldIcon!);

        if (_showCriminalRecord && TryComp<CriminalRecordComponent>(uid, out var criminalRecord) &&
            _prototype.TryIndex<StatusIconPrototype>(criminalRecord.StatusIcon.Id, out var criminalRecordIcon))
                result.Add(criminalRecordIcon);

        if (_showNukeOperative && HasComp<NukeOperativeComponent>(uid))
            result.Add(_nukeOperativeIcon!);

        ev.StatusIcons.AddRange(result);
    }

    private string GetJobIconIdFromAccessItems(EntityUid uid)
    {
        if (!_accessReader.FindAccessItemsInventory(uid, out var items))
            return JobIconNoId;

        foreach (var item in items)
        {
            // PDA
            if (TryComp<PdaComponent>(item, out var pda) && pda.ContainedId != null &&
                TryComp<IdCardComponent>(pda.ContainedId, out var idCard))
                    return idCard.JobIcon;

            // ID Card
            if (TryComp(item, out idCard))
                return idCard.JobIcon;
        }

        return JobIconNoId;
    }
}
