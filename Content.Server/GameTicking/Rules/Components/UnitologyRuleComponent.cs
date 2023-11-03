using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Storage;

namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
/// Component for the UnitologyRuleSystem that stores info about winning/losing, player counts required for starting, as well as prototypes for Uniolutionaries and their gear.
/// </summary>
[RegisterComponent, Access(typeof(UnitologyRuleSystem))]
public sealed partial class UnitologyRuleComponent : Component
{
     /// <summary>
    /// When the round will if all the command are dead (Incase they are in space)
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan CommandCheck;

    /// <summary>
    /// The amount of time between each check for command check.
    /// </summary>
    [DataField]
    public TimeSpan TimerWait = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Stores players minds
    /// </summary>
    [DataField]
    public Dictionary<string, EntityUid> Unis = new();

    [DataField]
    public ProtoId<AntagPrototype> UniPrototypeId = "Uni";

    /// <summary>
    /// Sound that plays when you are chosen as Uni. (Placeholder until I find something cool I guess)
    /// </summary>
    [DataField]
    public SoundSpecifier UniStartSound = new SoundPathSpecifier("/Audio/Ambience/Antag/unitolog_start.ogg");

    /// <summary>
    /// Min players needed for Uniolutionary gamemode to start.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MinPlayers = 15;

    /// <summary>
    /// Max  Unis allowed during selection.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int MaxUnis = 5;

    /// <summary>
    /// The amount of  Unis that will spawn per this amount of players.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public int PlayersPerUni = 15;

    /// <summary>
    /// The gear  Uniolutionaries are given on spawn.
    /// </summary>
    [DataField]
    public List<EntProtoId> StartingGear = new()
    {
        "SyringeExtractInfectorDead",
        "SyringeExtractInfectorDead",
        "EncryptionKeySyndie"
    };

    public bool ObeliskState = false;

}
