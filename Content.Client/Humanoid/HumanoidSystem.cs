using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

public sealed class HumanoidSystem : SharedHumanoidSystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MarkingManager _markingManager = default!;

    /// <summary>
    ///     Loads a profile directly into a humanoid.
    /// </summary>
    /// <param name="uid">The humanoid entity's UID</param>
    /// <param name="profile">The profile to load.</param>
    /// <param name="humanoid">The humanoid entity's humanoid component.</param>
    /// <remarks>
    ///     This should not be used if the entity is owned by the server. The server will otherwise
    ///     override this with the appearance data it sends over.
    /// </remarks>
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
            profile.Sex,
            new(), // doesn't exist yet
            markings.GetForwardEnumerator().ToList());
    }
}
