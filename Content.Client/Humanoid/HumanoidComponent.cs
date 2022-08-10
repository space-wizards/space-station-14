using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Species;
using Content.Shared.Markings;

namespace Content.Client.Humanoid;

public sealed class HumanoidComponent : SharedHumanoidComponent
{
    public List<Marking> CurrentMarkings = new();

    public Dictionary<HumanoidVisualLayers, HumanoidSpeciesSpriteLayer> BaseLayers = new();
}
