using Content.Shared.Atmos;

namespace Content.Server.Xenoarchaeology.Artifact.XAE.Components;


[RegisterComponent, Access(typeof(XAECreateGasSystem))]
public sealed partial class XAECreateGasComponent : Component
{
    /// <summary>
    /// The gases and how many moles will be created of each.
    /// </summary>
    [DataField]
    public Dictionary<Gas, float> Gases = new();
}
