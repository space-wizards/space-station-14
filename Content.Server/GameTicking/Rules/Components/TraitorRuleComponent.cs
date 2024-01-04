using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(TraitorRuleSystem))]
public sealed partial class TraitorRuleComponent : Component
{
    public readonly List<EntityUid> TraitorMinds = new();

    [DataField("traitorPrototypeId", customTypeSerializer: typeof(PrototypeIdSerializer<AntagPrototype>))]
    public string TraitorPrototypeId = "Traitor";

    public int TotalTraitors => TraitorMinds.Count;
    public string[] Codewords = new string[3];

    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public TimeSpan AnnounceAt = TimeSpan.Zero;

    /// <summary>
    ///     Path to antagonist alert sound.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/traitor_start.ogg");
}
