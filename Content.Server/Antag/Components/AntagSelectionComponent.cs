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

    public AntagSelectionTime SelectionTime = AntagSelectionTime.PostPlayerSpawn;

    public HashSet<ICommonSession> SelectedSessions = new();
}

[DataDefinition]
public partial struct AntagSelectionDefinition
{
    [DataField]
    public List<ProtoId<AntagPrototype>> PrefRoles = new();

    [DataField]
    public List<ProtoId<AntagPrototype>> FallbackRoles = new();

    [DataField]
    public AntagAcceptability MultiAntagSetting = AntagAcceptability.None;

    [DataField]
    public int MinAntags = 1;

    [DataField]
    public int MaxAntags = 1;

    //todo implement a max range thing? make both of these ranges?

    [DataField]
    public int PlayerRatio = 10;

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

    [DataField(serverOnly: true)]
    public ComponentRegistry Components = new();

    [DataField(serverOnly: true)]
    public ComponentRegistry MindComponents = new();

    [DataField]
    public List<EntProtoId> Equipment = new();

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
