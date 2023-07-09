using Content.Server.Roles;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(TraitorRuleSystem))]
public sealed class TraitorRuleComponent : Component
{
    public List<TraitorRole> Traitors = new();

    [DataField("traitorPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string TraitorPrototypeId = "Traitor";

    public int TotalTraitors => Traitors.Count;
    public string[] Codewords = new string[3];

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToSelect = 1,
        SelectionMade = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public TimeSpan AnnounceAt = TimeSpan.Zero;
    public Dictionary<IPlayerSession, HumanoidCharacterProfile> StartCandidates = new();

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
}
