using Content.Shared.Actions;
using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for entities which are bound to silicon laws and can view them.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class SiliconLawBoundComponent : Component
{
    /// <summary>
    /// The last entity that provided laws to this entity.
    /// </summary>
    [DataField]
    public EntityUid? LastLawProvider;

    /// <summary>
    /// <see cref="WeightedRandomPrototype"/> for a random lawset to possibly replace the old one with on an ion storm.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> IonRandomLawsets = "IonStormLawsets";

    /// <summary>
    /// Chance to replace the lawset with a random one on an ion storm.
    /// </summary>
    [DataField]
    public float IonRandomLawsetChance = 0.25f;

    /// <summary>
    /// Chance to remove a random law on an ion storm.
    /// </summary>
    [DataField]
    public float IonRemoveChance = 0.2f;

    /// <summary>
    /// Chance to replace a random law with the new one on an ion storm, rather than have it be a glitched-order law.
    /// </summary>
    [DataField]
    public float IonReplaceChance = 0.2f;

    /// <summary>
    /// Chance to shuffle laws after everything is done on an ion storm..
    /// </summary>
    [DataField]
    public float IonShuffleChance = 0.2f;
}

/// <summary>
/// Event raised to get the laws that a law-bound entity has.
///
/// Is first raised on the entity itself, then on the
/// entity's station, then on the entity's grid,
/// before being broadcast.
/// </summary>
/// <param name="Entity"></param>
[ByRefEvent]
public record struct GetSiliconLawsEvent(EntityUid Entity)
{
    public EntityUid Entity = Entity;

    public SiliconLawset Laws = new();

    public bool Handled = false;
}

/// <summary>
/// Raised on an ion storm target to modify its laws.
/// </summary>
[ByRefEvent]
public record struct IonStormLawsEvent(SiliconLawset Lawset);

public sealed partial class ToggleLawsScreenEvent : InstantActionEvent
{

}

[NetSerializable, Serializable]
public enum SiliconLawsUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class SiliconLawBuiState : BoundUserInterfaceState
{
    public List<SiliconLaw> Laws;
    public HashSet<string>? RadioChannels;

    public SiliconLawBuiState(List<SiliconLaw> laws, HashSet<string>? radioChannels)
    {
        Laws = laws;
        RadioChannels = radioChannels;
    }
}
