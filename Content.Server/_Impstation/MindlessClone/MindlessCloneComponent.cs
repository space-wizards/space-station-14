using Content.Shared.Dataset;
using Robust.Shared.Prototypes;
using Content.Shared.Cloning;

namespace Content.Server._Impstation.MindlessClone;
/// <summary>
/// When applied to an entity with HumanoidAppearance, copies the appearance data of the nearest entity with HumanoidAppearance when spawned.
/// </summary>
[RegisterComponent]
public sealed partial class MindlessCloneComponent : Component
{
    /// <summary>
    /// whether or not the entity will pick a randomized phrase to say after spawning. default false
    /// </summary>
    [DataField]
    public bool SpeakOnSpawn;

    /// <summary>
    /// a LocalizedDataset containing phrases for the clone to say if they SpeakOnSpawn
    /// </summary>
    [DataField]
    public ProtoId<LocalizedDatasetPrototype> PhrasesToPick = "MindlessCloneConfusion";

    /// <summary>
    /// whether or not the entity will mindswap with its cloning target. default false
    /// </summary>
    [DataField]
    public bool MindSwap;

    /// <summary>
    /// the amount of time in seconds that the clone gets stunned and muted on spawn. Also applies to mindswap targets.
    /// </summary>
    [DataField]
    public TimeSpan MindSwapStunTime = TimeSpan.FromSeconds(15);

    /// <summary>
    /// CloningSettingsPrototype. Currently only used to designate which basic components should be carried over to the clone
    /// (I.e. antags, bloodstream, etc)
    /// </summary>
    [DataField]
    public ProtoId<CloningSettingsPrototype> SettingsId = "MindlessClone";

    /// <summary>
    /// HashSet of strings containing the names of any components we want to be swapped with the cloning target during a mindswap.
    /// At the moment, we just use this to make sure the cloning target gets all the evil clone AI and whatnot.
    /// </summary>
    [DataField]
    public HashSet<string> ComponentsToSwap = new();

    /// <summary>
    /// Stores the entity this clone originated from. Mostly for passing that info between methods, but admins can probably use it too.
    /// </summary>
    public EntityUid IsCloneOf;

    /// <summary>
    /// Stores the original body of the clone, so we can access it for pointing after a mindswap.
    /// Is a datafield not because I expect people to assign it a value, but because otherwise CopyComp won't communicate it.
    /// </summary>
    [DataField]
    public EntityUid OriginalBody;

    public TimeSpan? NextDelayTime = null;

    public TimeSpan? NextSayTime = null;

    public string NextPhrase;
}
