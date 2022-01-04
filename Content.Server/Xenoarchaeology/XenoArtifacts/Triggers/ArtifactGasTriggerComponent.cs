using Content.Shared.Atmos;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.Xenoarchaeology.XenoArtifacts.Triggers;

[RegisterComponent]
public class ArtifactGasTriggerComponent : Component
{
    public override string Name => "ArtifactGasTrigger";

    [DataField("randomGas")]
    public bool RandomGas = true;

    [DataField("possibleGas")]
    public Gas[] PossibleGases =
    {
        Gas.Oxygen,
        Gas.Plasma,
        Gas.Nitrogen,
        Gas.CarbonDioxide
    };

    [DataField("gas")]
    [ViewVariables(VVAccess.ReadWrite)]
    public Gas? ActivationGas;

    [DataField("moles")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float ActivationMoles = Atmospherics.MolesCellStandard * 0.1f;
}
