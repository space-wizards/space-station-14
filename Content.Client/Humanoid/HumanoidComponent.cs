using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Markings;
using Content.Shared.Species;

namespace Content.Client.Humanoid;

public sealed class HumanoidComponent : SharedHumanoidComponent
{
    public List<Marking> CurrentMarkings = new();

    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers = new();
}
