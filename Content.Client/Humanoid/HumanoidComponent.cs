using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Client.Humanoid;

[RegisterComponent]
public sealed class HumanoidComponent : SharedHumanoidComponent
{
    [ViewVariables] public List<Marking> CurrentMarkings = new();

    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers = new();

    public string LastSpecies = default!;
}
