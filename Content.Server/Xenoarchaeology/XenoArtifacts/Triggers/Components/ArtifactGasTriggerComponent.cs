using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers.Components;

/// <summary>
///     Activates artifact when it surrounded by certain gas.
/// </summary>
[RegisterComponent]
public sealed class ArtifactGasTriggerComponent : Component
{
    /// <summary>
    ///     List of possible activation gases to pick on startup.
    /// </summary>
    [DataField("possibleGas")]
    public Gas[] PossibleGases =
    {
        Gas.Oxygen,
        Gas.Plasma,
        Gas.Nitrogen,
        Gas.CarbonDioxide
    };

    /// <summary>
    ///     Gas id that will activate artifact.
    /// </summary>
    [DataField("gas")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Gas? ActivationGas;

    /// <summary>
    ///     How many moles of gas should be present in room to activate artifact.
    /// </summary>
    [DataField("moles")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ActivationMoles = Atmospherics.MolesCellStandard * 0.1f;
}
