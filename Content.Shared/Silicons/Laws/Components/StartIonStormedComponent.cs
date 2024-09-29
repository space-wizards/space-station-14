using Content.Shared.Random;
using Robust.Shared.Prototypes;

namespace Content.Shared.Silicons.Laws.Components;

/// <summary>
/// Runs the IonStormSystem on an entity IonStormAmount times.
/// </summary>
[RegisterComponent]
public sealed partial class StartIonStormedComponent : Component
{
    /// <summary>
    /// Amount of times that the ion storm will be run on the entity on spawn.
    /// </summary>
    [DataField]
    public int IonStormAmount = 1;
}
