using Content.Shared.Random;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Applies law altering ion storms on a specific entity IonStormAmount times when the entity is spawned.
/// </summary>
[RegisterComponent]
public sealed partial class StartIonStormedComponent : Component
{
    /// <summary>
    /// Amount of times that the ion storm will be run on the entity on spawn.
    /// </summary>
    [DataField]
    public int IonStormAmount = 1;

    /// <summary>
    /// Which formats (and with what weights) will be applied to newly generated corrupted laws.
    /// </summary>
    [DataField]
    public ProtoId<WeightedRandomPrototype>? CorruptedLawFormattings;
}
