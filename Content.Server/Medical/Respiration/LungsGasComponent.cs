using Content.Shared.Atmos;

namespace Content.Server.Medical.Respiration;


[RegisterComponent]
public sealed partial class LungsGasComponent : Component
{
    //TODO: GasMixture is not in shared so I need to make a separate component to track this
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GasMixture ContainedGas = new();

}
