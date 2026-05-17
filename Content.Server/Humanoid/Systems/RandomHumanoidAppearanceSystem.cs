using Content.Server.Humanoid.Components;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Preferences;
using Robust.Shared.Random;

namespace Content.Server.Humanoid.Systems;

public sealed partial class RandomHumanoidAppearanceSystem : EntitySystem
{
    [Dependency] private HumanoidProfileSystem _humanoidProfile = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedVisualBodySystem _visualBody = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RandomHumanoidAppearanceComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RandomHumanoidAppearanceComponent component, MapInitEvent args)
    {
        // If we have an initial profile/base layer set, do not randomize this humanoid.
        if (!TryComp<HumanoidProfileComponent>(uid, out var humanoid))
            return;

        var profile = HumanoidCharacterProfile.RandomWithSpecies(humanoid.Species);

        _visualBody.ApplyProfileTo(uid, profile);
        _humanoidProfile.ApplyProfileTo(uid, profile);

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
