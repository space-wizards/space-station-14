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

        humanoid.Species = profile.Species;
        var customBaseLayers = new Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo>
        {
            [HumanoidVisualLayers.Eyes] = new CustomBaseLayerInfo(string.Empty, profile.Appearance.EyeColor)
        };

        var speciesPrototype = _prototypeManager.Index<SpeciesPrototype>(profile.Species);
        var markings = new MarkingSet(profile.Appearance.Markings, speciesPrototype.MarkingPoints, _markingManager,
            _prototypeManager);
        markings.EnsureDefault(profile.Appearance.SkinColor, _markingManager);

        // legacy: remove in the future?
        markings.RemoveCategory(MarkingCategories.Hair);
        markings.RemoveCategory(MarkingCategories.FacialHair);

        var hair = new Marking(profile.Appearance.HairStyleId, new[] { profile.Appearance.HairColor });
        markings.AddBack(MarkingCategories.Hair, hair);

        var facialHair = new Marking(profile.Appearance.FacialHairStyleId,
            new[] { profile.Appearance.FacialHairColor });
        markings.AddBack(MarkingCategories.FacialHair, facialHair);

        markings.FilterSpecies(profile.Species, _markingManager, _prototypeManager);

        SetAppearance(uid,
            profile.Species,
            customBaseLayers,
            profile.Appearance.SkinColor,
            new(), // doesn't exist yet
            markings.GetForwardEnumerator().ToList());
    }
}
