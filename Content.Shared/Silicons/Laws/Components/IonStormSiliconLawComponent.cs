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
    /// <see cref="WeightedRandomPrototype"/> for a random lawset to possibly replace the old one.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype> IonRandomLawsets = "IonStormLawsets";

    /// <summary>
    /// Chance to replace the lawset with a random one.
    /// </summary>
    [DataField]
    public float RandomLawsetChance = 0.25f;

    /// <summary>
    /// Chance to remove a random law.
    /// </summary>
    [DataField]
    public float RemoveChance = 0.2f;

    /// <summary>
    /// Chance to replace a random law with the new one, rather than have it be a glitched-order law.
    /// </summary>
    [DataField]
    public float ReplaceChance = 0.2f;

    /// <summary>
    /// Chance to shuffle laws after everything is done.
    /// </summary>
    [DataField]
    public float ShuffleChance = 0.2f;
}

/// <summary>
/// Raised on an ion storm target to modify its laws.
/// </summary>
[ByRefEvent]
public record struct IonStormLawsEvent(SiliconLawset Lawset);
