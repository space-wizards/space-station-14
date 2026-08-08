// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Shared.Access;
using Content.Shared.DeadSpace.PersonnelRecords.Systems;
using Content.Shared.DeadSpace.Photocopier;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Content.Shared.StationRecords;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.DeadSpace.PersonnelRecords.Components;

/// <summary>
/// The single Personnel Records console prototype (<c>ComputerPersonnelRecords</c>) placed on the
/// bridge and in every department head's office - one prototype, one board, one recipe. Visibility
/// scope is determined per-action from the acting player's ID card (see
/// <c>PersonnelRecordsConsoleSystem</c>), not from anything on this component.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SharedPersonnelRecordsConsoleSystem))]
public sealed partial class PersonnelRecordsConsoleComponent : Component
{
    /// <summary>
    /// Currently active station record key. Server-only bookkeeping, mirrored into the BUI state.
    /// </summary>
    [DataField]
    public uint? ActiveKey;

    /// <summary>
    /// Currently applied name/job/species/etc. search filter.
    /// </summary>
    [DataField]
    public StationRecordsFilter? Filter;

    /// <summary>
    /// Currently selected employment-status filter for the crew listing.
    /// </summary>
    [DataField]
    public EmploymentStatus FilterStatus;

    /// <summary>
    /// Access levels that grant visibility over the entire crew (minus exclusions), rather than
    /// just the acting player's own primary department.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> FullAccess = new()
    {
        "Captain",
        "HeadOfPersonnel",
    };

    /// <summary>
    /// Departments never shown, regardless of the acting player's access - AI/borgs, Central
    /// Command, Taipan and Special Operations Corps personnel are outside this console's scope
    /// entirely.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> BlacklistedDepartments = new()
    {
        "Silicon",
        "CentralCommand",
        "Taipan",
        "SpecialOperationsCorps",
    };

    /// <summary>
    /// Jobs never shown, regardless of department: "nothing left to demote/dismiss" (Passenger,
    /// Visitor, Dismissed) plus Magistrat, who answers to CentCom directly and never appears here
    /// at all. Captain, IAA and BlueShieldOfficer are deliberately *not* in this list - they're
    /// visible/browsable (see <see cref="ProtectedJobs"/>), just untouchable without the right
    /// access.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> ExcludedJobs = new()
    {
        "Passenger",
        "Visitor",
        "Dismissed",
        "Magistrat",
    };

    /// <summary>
    /// Jobs that can only be acted against by someone with <see cref="ProtectedJobsAccess"/> - the
    /// captain, the blue shield officer and the IAA agent answer to Central Command, not to each
    /// other or to the HoP. Checked before <see cref="ProtectedDepartments"/>, so it wins even for a
    /// job that would otherwise fall under a protected department.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> ProtectedJobs = new()
    {
        "Captain",
        "IAA",
        "BlueShieldOfficer",
    };

    /// <summary>
    /// Access required to act against someone in <see cref="ProtectedJobs"/>.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype> ProtectedJobsAccess = "CentralCommand";

    /// <summary>
    /// Departments whose members can only be disciplined by someone with <see cref="ProtectedAccess"/>
    /// - i.e. department heads can only be disciplined by the captain, never by the HoP.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> ProtectedDepartments = new()
    {
        "Command",
    };

    /// <summary>
    /// Access required to act against someone in a <see cref="ProtectedDepartments"/> department.
    /// </summary>
    [DataField]
    public ProtoId<AccessLevelPrototype> ProtectedAccess = "Captain";

    /// <summary>
    /// Access levels allowed to declare someone wanted from this console. This is a Security action
    /// being triggered from a Personnel Records console, not a personnel-status transition, so it's
    /// deliberately its own list rather than reusing <see cref="FullAccess"/> - the HoP has full
    /// visibility over the crew but has no business declaring anyone wanted.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> DeclareWantedAccess = new()
    {
        "Captain",
        "HeadOfSecurity",
    };

    /// <summary>
    /// Whether executing a Demotion also returns the vacated job's slot to the station's pool, same
    /// as a Dismissal does.
    /// </summary>
    [DataField]
    public bool FreeSlotOnDemotion = true;

    /// <summary>
    /// Department -> radio channel used for disciplinary announcements. Not derivable from
    /// <see cref="DepartmentPrototype"/> itself (it carries no channel field), and department/channel
    /// names don't always match (Cargo -> Supply, Civilian -> Service).
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<DepartmentPrototype>, ProtoId<RadioChannelPrototype>> DepartmentChannels = new()
    {
        { "Engineering", "Engineering" },
        { "Medical", "Medical" },
        { "Science", "Science" },
        { "Cargo", "Supply" },
        { "Security", "Security" },
        { "Civilian", "Service" },
        { "Law", "Law" },
    };

    /// <summary>
    /// Radio channel that additionally receives every Demotion/Dismissal, cancellation and
    /// execution announcement, since Security ends up escorting the person either way.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> SecurityChannel = "Security";

    /// <summary>
    /// Max length of reason strings.
    /// </summary>
    [DataField]
    public uint MaxStringLength = 256;

    /// <summary>
    /// Minimum time between state-changing actions on this console (issue/annul/declare wanted),
    /// to make "discipline the whole department in a minute" at least require some effort.
    /// </summary>
    [DataField]
    public TimeSpan ActionDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// The time at which this console will accept another state-changing action.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextActionTime = TimeSpan.Zero;

    /// <summary>
    /// The discipline order form printed by the "Распечатать приказ" button. Fixed to a single
    /// prototype rather than a category-driven list like the photocopier - the console always
    /// prints exactly this one thing.
    /// </summary>
    [DataField]
    public ProtoId<PaperworkFormPrototype> OrderForm = "PersonnelDiscipline";

    /// <summary>
    /// Sound played when the order form is printed.
    /// </summary>
    [DataField]
    public SoundSpecifier PrintSound = new SoundCollectionSpecifier("PrinterPrint");

    /// <summary>
    /// Minimum time between prints, same idea as <see cref="CargoOrderConsoleComponent.PrintDelay"/>
    /// - without it the console turns into a machine for burying the bridge in paper.
    /// </summary>
    [DataField]
    public TimeSpan PrintDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// The time at which this console will accept another print.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPrintTime = TimeSpan.Zero;
}
