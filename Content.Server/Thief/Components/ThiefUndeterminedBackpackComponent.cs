using Content.Server.Thief.Systems;
using Content.Shared.Thief;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Thief.Components;

/// <summary>
/// This component stores the possible contents of the backpack,
/// which can be selected via the interface.
/// </summary>
[RegisterComponent, Access(typeof(ThiefUndeterminedBackpackSystem))]
public sealed partial class ThiefUndeterminedBackpackComponent : Component
{
    /// <summary>
    /// List of sets available for selection
    /// </summary>
    [DataField]
    public List<ProtoId<ThiefBackpackSetPrototype>> PossibleSets = new();

    [DataField]
    public List<int> SelectedSets = new();

    [DataField]
    public SoundCollectionSpecifier ApproveSound = new SoundCollectionSpecifier("storageRustle");

    /// <summary>
    /// Max number of sets you can select.
    /// </summary>
    [DataField]
    public int MaxSelectedSets = 2;

    /// <summary>
    /// What entity all the spawned items will appear inside of
    /// If null, will instead drop on the ground.
    /// </summary>
    [DataField]
    public EntProtoId? SpawnedStoragePrototype;
}
