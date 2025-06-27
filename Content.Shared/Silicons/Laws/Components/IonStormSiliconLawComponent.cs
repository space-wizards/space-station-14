using Content.Shared.Random;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// This is used for an entity which has its laws adjusted on an ion storm.
/// </summary>
/// <remarks>
/// Note: You'll also need to give the entity <see cref="IonStormTargetComponent"/> for it to be affected.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(SharedSiliconLawSystem))]
public sealed partial class IonStormSiliconLawComponent : Component
{
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
/// Raised on an ion storm target to modify its laws.
/// </summary>
[ByRefEvent]
public record struct IonStormLawsEvent(SiliconLawset Lawset);
