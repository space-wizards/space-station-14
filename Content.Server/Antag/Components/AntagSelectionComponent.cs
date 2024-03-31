using Content.Server.Destructible.Thresholds;
using Content.Shared.Antag;
using Content.Shared.Roles;
using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Antag.Components;

[RegisterComponent]
public sealed partial class AntagSelectionComponent : Component
{
    [DataField]
    public bool SelectionsComplete;

    [DataField]
    public List<AntagSelectionDefinition> Definitions = new();

    [DataField]
    public List<(EntityUid, string)> SelectedMinds = new();

    [DataField]
    public AntagSelectionTime SelectionTime = AntagSelectionTime.PostPlayerSpawn;

    public HashSet<ICommonSession> SelectedSessions = new();
}

[DataDefinition]
public partial struct AntagSelectionDefinition()
{
    [DataField]
    public List<ProtoId<AntagPrototype>> PrefRoles = new();

    [DataField]
    public List<ProtoId<AntagPrototype>> FallbackRoles = new();

    [DataField]
    public AntagAcceptability MultiAntagSetting = AntagAcceptability.None;

    [DataField]
    public int Min = 1;

    [DataField]
    public int Max = 1;

    [DataField]
    public MinMax? MinRange;

    [DataField]
    public MinMax? MaxRange;

    [DataField]
    public int PlayerRatio = 10;

    /// <summary>
    /// If true, players that latejoin into a round have a chance of being converted into antagonists.
    /// </summary>
    [DataField]
    public bool LateJoinAdditional = false;

    //todo: find out how to do this with minimal boilerplate: filler department, maybe?
    //public HashSet<ProtoId<JobPrototype>> JobBlacklist = new()

    /// <remarks>
    /// Mostly just here for legacy compatibility and reducing boilerplate
    /// </remarks>
    [DataField]
    public bool AllowNonHumans = false;

    [DataField]
    public EntityWhitelist? Whitelist;

    [DataField]
    public EntityWhitelist? Blacklist;

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public ComponentRegistry MindComponents = new();

    [DataField]
    public List<EntProtoId> Equipment = new();

    [DataField]
    public ProtoId<StartingGearPrototype>? StartingGear;

    [DataField]
    public BriefingData? Briefing;
}

[DataDefinition]
public partial struct BriefingData
{
    [DataField]
    public LocId Text;

    [DataField]
    public Color? Color;

    [DataField]
    public SoundSpecifier? Sound;
}
