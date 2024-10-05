using Content.Shared.Roles;//Used 
using Robust.Shared.Prototypes;//

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
    /// A role given to entities with this component when they are thing-that-is-not-emagged.
    /// Mostly just for admin purposes.
    /// </summary>
    [DataField]
    public ProtoId<AntagPrototype>? AntagonistRole = "SubvertedSilicon";
}
