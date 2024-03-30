using Content.Server.Objectives.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Content.Shared.Roles;
using Robust.Shared.Player;
using Content.Shared.Preferences;
using Robust.Shared.Audio;
using Content.Server.Objectives.Components;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Random;
using Robust.Shared.Prototypes;
namespace Content.Server.GameTicking.Rules.Components;

/// <summary>
///     This component holds data about the Obsessed antag, such as the sound to play, etc.
///     It also tracks how many people are Obsessed
/// </summary>

[RegisterComponent] //register component here means that it uses the yaml
public sealed partial class ObsessedRuleComponent : Component
{
    [DataField]
    public ProtoId<WeightedRandomPrototype> ObjectiveGroup = "ObsessedObjectiveGroups";



    public readonly List<EntityUid> ObsessedMinds = new();

    [DataField]
    public string ObsessedPrototypeId = "Obsessed";

    public Dictionary<ICommonSession, HumanoidCharacterProfile> StartCandidates = new();
    public int TotalObsessed => ObsessedMinds.Count;
    public enum SelectionState
    {
        WaitingForSpawn = 0,
        ReadyToStart = 1,
        Started = 2,
    }

    public SelectionState SelectionStatus = SelectionState.WaitingForSpawn;
    public TimeSpan AnnounceAt = TimeSpan.Zero;

    /// <summary>
    ///     Path to the Obsessed antagonist alert sound.
    ///     This will play when they become obsessed.
    /// </summary>
    [DataField("greetSoundNotification")]
    public SoundSpecifier GreetSoundNotification = new SoundPathSpecifier("/Audio/Ambience/Antag/obsessed_start.ogg");

}

//Leaving all my hug target stuff out for now - will revisit once code for "hugging" actually exists in usable form for this objective
//  there's currently no way to track when someone hugs someone else, without doing an overhaul

//[ByRefEvent] //this is an event that, once called from code, will send this data to any listeners/subscribers
//public readonly record struct HugTargetConditionAddedEvent(EntityUid MindID, HugTargetConditionComponent HugConditionComponent);
