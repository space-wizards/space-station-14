using System.Linq;
using Content.Server.CharacterAppearance.Components;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Random;

namespace Content.Server.Humanoid.Systems;

public sealed class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp(uid, out HumanoidAppearanceComponent? humanoid) || !string.IsNullOrEmpty(humanoid.Initial))
        {
            return;
        }

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);
        var appearance = profile.Appearance; // imp

        // imp edits start
        List<Marking> markings;
        if (component.Markings != null)
            markings = MarkingsToAdd(component.Markings);
        else
            markings = appearance.Markings;


        var finalAppearance = new HumanoidCharacterAppearance(
            component.Hair ?? appearance.HairStyleId,
            component.HairColor ?? appearance.HairColor,
            component.FacialHair ?? appearance.FacialHairStyleId,
            component.HairColor ?? appearance.HairColor,
            component.EyeColor ?? appearance.EyeColor,
            component.SkinColor ?? appearance.SkinColor,
            markings
        );

        var finalProfile = new HumanoidCharacterProfile()
        {
            Name = profile.Name,
            Age = component.Age ?? profile.Age,
            Species = humanoid.Species,
            Appearance = finalAppearance
        }
        .WithSex(component.Sex ?? profile.Sex)
        .WithGender(component.Gender ?? profile.Gender);

        _humanoid.LoadProfile(uid, finalProfile, humanoid);
        // imp edits end


        if (component.RandomizeName)
            _metaData.SetEntityName(uid, profile.Name);
    }

    private List<Marking> MarkingsToAdd(Dictionary<string, List<Color>> dict)
    {
        List<Marking> output = [];
        var randomizeColor = new Color(
        _random.NextFloat(1),
        _random.NextFloat(1),
        _random.NextFloat(1));

        foreach (var keyValuePair in dict)
        {
            List<Color> markingColors = [];
            // if the list<color> has no members, set it to our random color. otherwise, set it to the color in the list.
            if (keyValuePair.Value.Count <= 0)
            {
                markingColors.Add(randomizeColor);
            }
            else
            {
                foreach (var colors in keyValuePair.Value)
                {
                    markingColors.Add(colors);
                }
            }

            var toAdd = new Marking(keyValuePair.Key, markingColors);
            output.Add(toAdd);
        }
        return output;
    }
}
