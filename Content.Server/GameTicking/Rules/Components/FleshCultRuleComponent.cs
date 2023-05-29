using Content.Server.Flesh;
using Content.Server.NPC.Components;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(FleshCultRuleSystem))]
public sealed class FleshCultRuleComponent : Component
{
    public SoundSpecifier AddedSound = new SoundPathSpecifier(
        "/Audio/Animals/Flesh/flesh_culstis_greeting.ogg");

    public SoundSpecifier BuySuccesSound = new SoundPathSpecifier(
        "/Audio/Animals/Flesh/flesh_cultist_buy_succes.ogg");

    public List<FleshCultistRole> Cultists = new();

    [DataField("fleshCultistPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string FleshCultistPrototypeId = "FleshCultist";

    [DataField("fleshCultistLeaderPrototypeID", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string FleshCultistLeaderPrototypeId = "FleshCultistLeader";

    [DataField("faction", customTypeSerializer: typeof(PrototypeIdSerializer<FactionPrototype>), required: true)]
    public string Faction = default!;

    public int TotalCultists => Cultists.Count;

    public readonly List<string> CultistsNames = new();

    public WinTypes WinType = WinTypes.Fail;

    public EntityUid? TargetStation;

    public List<string> SpeciesWhitelist = new()
    {
        "Human",
        "Reptilian",
        "Dwarf",
    };

    public enum WinTypes
    {
        FleshHeartFinal,
        Fail
    }

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToSelect = 1,
        SelectionMade = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public Dictionary<IPlayerSession, HumanoidCharacterProfile> StartCandidates = new();
}
