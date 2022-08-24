using System.Linq;
using Content.Shared.CharacterAppearance;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Species;
using Content.Shared.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

public sealed class HumanoidSystem : SharedHumanoidSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    public void LoadProfile(EntityUid uid, HumanoidCharacterProfile profile, HumanoidComponent? humanoid = null)
    {
        if (!Resolve(uid, ref humanoid))
        {
            return;
        }

        var customBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>
        {
            [HumanoidVisualLayers.Eyes] = new CustomBaseLayerInfo(string.Empty, profile.Appearance.EyeColor)
        };

        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var markings = new MarkingSet(profile.Appearance.Markings, speciesPrototype.MarkingPoints, _markingManager,
            _prototypeManager);
        markings.EnsureDefault(profile.Appearance.SkinColor, _markingManager);

        SetAppearance(uid,
            profile.Species,
            customBaseLayers,
            profile.Appearance.SkinColor,
            new(), // doesn't exist yet
            markings.GetForwardEnumerator().ToList());
    }
}
